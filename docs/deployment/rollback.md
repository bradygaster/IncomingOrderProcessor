# Deployment Rollback Procedures

This document provides detailed procedures for rolling back deployments in case of issues.

## Overview

The IncomingOrderProcessor application uses Azure App Service for hosting. Azure provides several mechanisms for rolling back deployments, including deployment slots and artifact-based rollbacks.

## Quick Rollback (Production)

### Method 1: Deployment Slot Swap (Recommended)

If you have deployment slots configured:

```bash
# Swap production with the previous slot
az webapp deployment slot swap \
  --name <app-name> \
  --resource-group <resource-group> \
  --slot production \
  --target-slot backup-<previous-commit-sha>
```

### Method 2: Redeploy Previous Version

1. Navigate to GitHub Actions in the repository
2. Find the last successful CD workflow run before the problematic deployment
3. Click on the workflow run
4. Click "Re-run all jobs"
5. Select "production" environment when prompted

### Method 3: Manual Rollback via Azure Portal

1. Log in to [Azure Portal](https://portal.azure.com)
2. Navigate to the App Service instance
3. Go to **Deployment Center** > **Logs**
4. Find the previous successful deployment
5. Click **Redeploy** on that deployment

## Environment-Specific Rollback Procedures

### Development Environment

Development rollbacks are typically straightforward as there are no end users affected:

```bash
# Identify the last working commit
git log --oneline

# Trigger a redeployment from that commit
gh workflow run cd.yml --ref <commit-sha> -f environment=dev
```

Or redeploy via GitHub Actions UI:
1. Navigate to Actions tab
2. Select the CD workflow
3. Click "Run workflow"
4. Select the branch/tag to deploy
5. Choose "dev" environment

### Staging Environment

Staging rollbacks should be performed carefully as they may affect testing:

```bash
# Find the last successful deployment
gh run list --workflow=cd.yml --status=success --limit=10

# Redeploy from a specific run
gh workflow run cd.yml --ref <commit-sha> -f environment=staging
```

**Post-Rollback Steps**:
1. Verify staging environment is functioning correctly
2. Notify QA team of the rollback
3. Re-run smoke tests
4. Document the reason for rollback

### Production Environment

Production rollbacks require careful coordination:

**Before Rolling Back**:
1. Notify all stakeholders (engineering, product, support)
2. Create an incident ticket/report
3. Identify the root cause if possible
4. Determine if a rollback is necessary or if a hotfix is more appropriate

**Rollback Steps**:

```bash
# Option 1: Use Azure CLI to swap slots (fastest)
az webapp deployment slot swap \
  --name $AZURE_APP_NAME_PROD \
  --resource-group $AZURE_RESOURCE_GROUP \
  --slot production \
  --target-slot backup-<last-good-sha>

# Option 2: Redeploy previous version via GitHub Actions
gh workflow run cd.yml --ref <last-good-commit> -f environment=production
```

**Post-Rollback Steps**:
1. Verify production is functioning correctly
2. Run production smoke tests
3. Monitor error rates and performance metrics
4. Update status page if applicable
5. Send notification to all stakeholders
6. Schedule post-mortem meeting
7. Document lessons learned

## Rollback Decision Matrix

Use this matrix to decide on the appropriate rollback strategy:

| Severity | Impact | Action | Approval Required |
|----------|--------|--------|-------------------|
| Critical | Service down | Immediate rollback via slot swap | Engineering Lead (can be retrospective) |
| High | Major feature broken | Rollback via redeployment | Engineering Lead + Product Owner |
| Medium | Minor feature broken | Evaluate hotfix vs rollback | Team decision |
| Low | Cosmetic issue | Schedule fix in next deployment | No rollback needed |

## Automated Rollback

The CD workflow includes automatic backup creation before production deployments. Each deployment creates a backup slot named `backup-<commit-sha>`.

### Enabling Automatic Rollback on Failure

To enable automatic rollback on health check failures, configure Azure App Service health checks:

1. Go to Azure Portal > App Service
2. Navigate to **Health check**
3. Configure health check endpoint (e.g., `/health`)
4. Set health check interval to 1 minute
5. Enable **Auto heal** in **Diagnose and solve problems**

## Database Rollback Considerations

⚠️ **Warning**: The IncomingOrderProcessor may include database changes. Always consider:

1. **Backward Compatibility**: Ensure database changes are backward compatible
2. **Data Migration**: If schema changes were made, rolling back may require database restoration
3. **Backup Verification**: Always verify database backups before major deployments

### Database Rollback Steps

If database changes were included:

```bash
# Restore database from backup (if needed)
az sql db restore \
  --resource-group <resource-group> \
  --server <sql-server-name> \
  --name <database-name> \
  --dest-name <database-name>-restore \
  --time "YYYY-MM-DDTHH:MM:SS"

# Update connection strings to point to restored database
az webapp config connection-string set \
  --name <app-name> \
  --resource-group <resource-group> \
  --settings DefaultConnection="<connection-string>"
```

## Monitoring During Rollback

Monitor these metrics during and after rollback:

1. **Application Health**
   - HTTP response codes (aim for <1% 5xx errors)
   - Response times (should return to baseline)
   - Request rate (should stabilize)

2. **Azure App Service Metrics**
   ```bash
   # View recent metrics
   az monitor metrics list \
     --resource <resource-id> \
     --metric "Http5xx,ResponseTime,Requests"
   ```

3. **Application Insights**
   - Exception rate
   - Failed request rate
   - Custom events/metrics

## Communication Templates

### Rollback Notification Template

```
Subject: [PRODUCTION] Rollback Initiated - IncomingOrderProcessor

Team,

A rollback has been initiated for the IncomingOrderProcessor application.

- Environment: Production
- Reason: [Brief description]
- Rollback initiated by: [Name]
- Rollback initiated at: [Timestamp]
- Rolling back to version: [Commit SHA/Version]
- Expected completion: [Timestamp]

We will provide updates as the rollback progresses.

- DevOps Team
```

### Rollback Completion Template

```
Subject: [PRODUCTION] Rollback Completed - IncomingOrderProcessor

Team,

The rollback for IncomingOrderProcessor has been completed successfully.

- Environment: Production
- Rolled back to version: [Commit SHA/Version]
- Rollback completed at: [Timestamp]
- Service status: Operational

Post-mortem meeting scheduled for: [Date/Time]

- DevOps Team
```

## Prevention Best Practices

To minimize the need for rollbacks:

1. **Testing**
   - Run comprehensive tests in CI pipeline
   - Perform thorough testing in staging
   - Use feature flags for risky changes

2. **Deployment Strategy**
   - Always deploy to dev first
   - Require staging approval before production
   - Use blue-green or canary deployments for major changes

3. **Monitoring**
   - Set up proactive alerts
   - Monitor key metrics continuously
   - Implement health checks

4. **Documentation**
   - Document all changes in commit messages
   - Update release notes
   - Maintain runbooks for common issues

## Rollback Testing

Regularly test rollback procedures:

1. **Quarterly Rollback Drills**
   - Schedule a rollback drill every quarter
   - Use staging environment for practice
   - Time the rollback process
   - Document any issues encountered

2. **Rollback Checklist**
   - [ ] Stakeholders notified
   - [ ] Root cause identified
   - [ ] Rollback method selected
   - [ ] Database impact assessed
   - [ ] Rollback executed
   - [ ] Health checks passed
   - [ ] Monitoring confirmed normal operation
   - [ ] Stakeholders notified of completion
   - [ ] Post-mortem scheduled

## Emergency Contacts

| Role | Contact | Availability |
|------|---------|--------------|
| Engineering Lead | [Contact Info] | 24/7 |
| DevOps Lead | [Contact Info] | 24/7 |
| Product Owner | [Contact Info] | Business hours |
| Azure Support | Azure Portal Support | 24/7 |

## References

- [Azure App Service Deployment Best Practices](https://docs.microsoft.com/en-us/azure/app-service/deploy-best-practices)
- [GitHub Actions Rerun Documentation](https://docs.github.com/en/actions/managing-workflow-runs/re-running-workflows-and-jobs)
- [Azure App Service Deployment Slots](https://docs.microsoft.com/en-us/azure/app-service/deploy-staging-slots)
