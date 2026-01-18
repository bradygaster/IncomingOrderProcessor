# Modernization Configuration

This directory contains your organization's modernization preferences and generated artifacts.

## Configuration Files (Your Overrides)

- **playbook.yaml** - Your migration strategy overrides (Microsoft defaults apply for anything not specified)
- **enterprise-policies.yaml** - Your organization's policy overrides (optional)

## Generated Artifacts

- **assessment.json** - Machine-readable assessment results
- **ASSESSMENT.md** - Human-readable assessment report
- **tasks/** - Individual migration task files

## Usage

The configuration files are automatically used by `modernize` commands:

```bash
# Uses your playbook + policies automatically
modernize assess .
modernize plan .
modernize apply .
```

## Reconfiguring

To change preferences:

```bash
# Interactive reconfiguration
modernize assess . --reconfigure
```

## Editing Manually

You can edit `playbook.yaml` and `enterprise-policies.yaml` directly. Changes take effect on the next command.

**These files contain ONLY your overrides.** Microsoft defaults are loaded automatically from the tool's config.

Common playbook customizations:
- `migration_strategy.framework.target` - Target .NET version
- `migration_strategy.compute.preferred` - Compute target (container_apps, app_service, aks)
- `iac.format` - Infrastructure as Code format (bicep, terraform, arm)

## Documentation

- [Playbook Schema Reference](https://aka.ms/appmodagent/playbook-schema)
- [AppModAgent Documentation](https://aka.ms/appmodagent)
