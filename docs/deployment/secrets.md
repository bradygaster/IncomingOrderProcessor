# GitHub Secrets Configuration

This document outlines the required GitHub secrets for the CI/CD pipeline.

## Required Secrets

### Azure Authentication

#### `AZURE_CREDENTIALS`
- **Description**: Service principal credentials for Azure authentication
- **Format**: JSON object
- **Required for**: All deployment workflows (dev, staging, production)
- **How to create**:
  ```bash
  az ad sp create-for-rbac --name "IncomingOrderProcessor-GitHub-Actions" \
    --role contributor \
    --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group} \
    --sdk-auth
  ```
- **Expected format**:
  ```json
  {
    "clientId": "<GUID>",
    "clientSecret": "<STRING>",
    "subscriptionId": "<GUID>",
    "tenantId": "<GUID>",
    "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
    "resourceManagerEndpointUrl": "https://management.azure.com/",
    "activeDirectoryGraphResourceId": "https://graph.windows.net/",
    "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
    "galleryEndpointUrl": "https://gallery.azure.com/",
    "managementEndpointUrl": "https://management.core.windows.net/"
  }
  ```

#### `AZURE_SUBSCRIPTION_ID`
- **Description**: Azure subscription ID
- **Format**: GUID (e.g., `12345678-1234-1234-1234-123456789abc`)
- **Required for**: Resource management operations
- **How to find**:
  ```bash
  az account show --query id -o tsv
  ```

#### `AZURE_RESOURCE_GROUP`
- **Description**: Azure resource group name containing the application resources
- **Format**: String (e.g., `rg-incomingorderprocessor-prod`)
- **Required for**: Deployment operations

### Application Secrets

#### `AZURE_APP_NAME_DEV`
- **Description**: Azure App Service name for development environment
- **Format**: String (e.g., `app-incomingorderprocessor-dev`)
- **Required for**: Development deployments

#### `AZURE_APP_NAME_STAGING`
- **Description**: Azure App Service name for staging environment
- **Format**: String (e.g., `app-incomingorderprocessor-staging`)
- **Required for**: Staging deployments

#### `AZURE_APP_NAME_PROD`
- **Description**: Azure App Service name for production environment
- **Format**: String (e.g., `app-incomingorderprocessor-prod`)
- **Required for**: Production deployments

## Setting Up Secrets

### Via GitHub Web Interface

1. Navigate to your repository on GitHub
2. Go to **Settings** > **Secrets and variables** > **Actions**
3. Click **New repository secret**
4. Enter the secret name and value
5. Click **Add secret**

### Via GitHub CLI

```bash
# Set Azure credentials
gh secret set AZURE_CREDENTIALS < azure-credentials.json

# Set subscription ID
gh secret set AZURE_SUBSCRIPTION_ID --body "your-subscription-id"

# Set resource group
gh secret set AZURE_RESOURCE_GROUP --body "your-resource-group-name"

# Set app names
gh secret set AZURE_APP_NAME_DEV --body "your-dev-app-name"
gh secret set AZURE_APP_NAME_STAGING --body "your-staging-app-name"
gh secret set AZURE_APP_NAME_PROD --body "your-prod-app-name"
```

## Environment-Specific Secrets

For better security and organization, you can also configure secrets at the environment level:

1. Navigate to **Settings** > **Environments**
2. Create environments: `dev`, `staging`, `production`
3. Add environment-specific secrets to each environment

### Environment Protection Rules

Configure the following protection rules for each environment:

#### Development (`dev`)
- No protection rules required
- Automatic deployment on merge to main

#### Staging (`staging`)
- Required reviewers: DevOps team members
- Wait timer: 0 minutes
- Deployment branches: `main` only

#### Production (`production`)
- Required reviewers: Senior DevOps and Project Lead
- Wait timer: 0 minutes
- Deployment branches: `main` only
- Require manual approval before deployment

## Security Best Practices

1. **Rotate credentials regularly**: Update service principal credentials every 90 days
2. **Use least privilege**: Ensure service principals have minimum required permissions
3. **Audit access**: Regularly review who has access to secrets
4. **Use environment secrets**: Where possible, use environment-level secrets instead of repository-level
5. **Never commit secrets**: Ensure secrets are never committed to the repository
6. **Monitor usage**: Enable logging and monitoring for service principal usage

## Troubleshooting

### Authentication Failures

If you encounter authentication failures:

1. Verify the service principal credentials are correct
2. Check that the service principal has not expired
3. Ensure the service principal has the correct role assignments
4. Verify the subscription ID and resource group names are correct

### Missing Secrets

If a workflow fails due to missing secrets:

1. Check the workflow logs for the specific secret that's missing
2. Verify the secret name matches exactly (names are case-sensitive)
3. Ensure the secret is set at the repository or environment level
4. Confirm you have the necessary permissions to view/edit secrets

## References

- [GitHub Actions Secrets Documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Azure Service Principal Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal)
- [Azure Login Action Documentation](https://github.com/Azure/login)
