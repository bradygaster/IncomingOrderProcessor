# Migration Guide: .NET Framework 4.8.1 to .NET 10

## Overview

This document outlines the migration of IncomingOrderProcessor from .NET Framework 4.8.1 Windows Service to .NET 10 Worker Service.

## Pre-Migration State

### Technology Stack
- Framework: .NET Framework 4.8.1
- Architecture: Windows Service
- Messaging: Microsoft Message Queue (MSMQ)
- Serialization: XML
- Platform: Windows-only

### Issues
- 11 Service Control Manager API calls (mandatory fix)
- 10 MSMQ usage locations (mandatory fix)
- Windows-only deployment
- No cloud compatibility

## Post-Migration State

### Technology Stack
- Framework: .NET 10
- Architecture: Worker Service (BackgroundService)
- Messaging: Azure Storage Queues
- Serialization: JSON
- Platform: Cross-platform (Windows, Linux, macOS)

### Benefits
✅ All mandatory issues resolved  
✅ Cloud-ready architecture  
✅ Cross-platform compatibility  
✅ Modern dependency injection  
✅ Structured logging  
✅ Configuration flexibility  
✅ Better testability  

## Deployment Options

### 1. Console Application (Development)
```bash
cd IncomingOrderProcessor
dotnet run
```

### 2. Windows Service
```bash
# Publish the application
dotnet publish -c Release -o ./publish

# Create Windows Service
sc create IncomingOrderProcessor binPath="C:\path\to\publish\IncomingOrderProcessor.exe"
sc start IncomingOrderProcessor
```

### 3. Linux systemd Service
```bash
# Publish for Linux
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish

# Create systemd service file at /etc/systemd/system/incomingorderprocessor.service
# [Service]
# ExecStart=/path/to/publish/IncomingOrderProcessor
# Restart=always

sudo systemctl daemon-reload
sudo systemctl enable incomingorderprocessor
sudo systemctl start incomingorderprocessor
```

### 4. Docker Container
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY ./publish .
ENTRYPOINT ["dotnet", "IncomingOrderProcessor.dll"]
```

### 5. Azure Container Apps
```bash
# Build and push container
az acr build --registry <registry-name> --image incomingorderprocessor:latest .

# Deploy to Container Apps
az containerapp create \
  --name incomingorderprocessor \
  --resource-group <resource-group> \
  --environment <environment> \
  --image <registry-name>.azurecr.io/incomingorderprocessor:latest
```

### 6. Azure App Service
```bash
dotnet publish -c Release
az webapp deploy --resource-group <rg> --name <app-name> --src-path ./publish
```

## Configuration Changes

### Before (App.config)
```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <startup> 
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8.1" />
    </startup>
</configuration>
```

### After (appsettings.json)
```json
{
  "OrderProcessor": {
    "QueueConnectionString": "UseDevelopmentStorage=true",
    "QueueName": "productcatalogorders"
  }
}
```

## Code Changes Summary

### Files Removed
- `App.config`
- `Properties/AssemblyInfo.cs`
- `Service1.Designer.cs`
- `ProjectInstaller.cs`
- `ProjectInstaller.Designer.cs`
- `ProjectInstaller.resx`

### Files Added
- `OrderProcessorWorker.cs` (replaces Service1.cs)
- `appsettings.json`
- `appsettings.Development.json`
- `README.md`
- `MIGRATION.md` (this file)

### Files Modified
- `IncomingOrderProcessor.csproj` (SDK-style)
- `Program.cs` (minimal hosting model)
- `Order.cs` (removed [Serializable])

## Dependencies Changed

### Removed
- System.ServiceProcess
- System.Messaging
- System.Configuration.Install
- System.Management

### Added
- Microsoft.Extensions.Hosting (10.0.1)
- Azure.Storage.Queues (12.24.0)

## Testing & Validation

All checks passed:
- ✅ Build: Success (0 errors, 0 warnings)
- ✅ Security scan: 0 vulnerabilities
- ✅ Code review: No issues
- ✅ CodeQL: 0 alerts
- ✅ Application startup: Verified

## Queue Migration Notes

### MSMQ Message Format (Before)
```xml
<?xml version="1.0"?>
<Order>
  <OrderId>123</OrderId>
  <OrderDate>2024-01-01T00:00:00</OrderDate>
  <!-- ... -->
</Order>
```

### Azure Storage Queue Format (After)
```json
{
  "OrderId": "123",
  "OrderDate": "2024-01-01T00:00:00",
  "Items": [],
  "Subtotal": 0,
  "Tax": 0,
  "Shipping": 0,
  "Total": 0,
  "CustomerSessionId": ""
}
```

## Rollback Plan

If rollback is needed:
1. The original .NET Framework 4.8.1 code is preserved in git history
2. Use `git revert` to undo the migration commit
3. Rebuild with .NET Framework 4.8.1 SDK
4. Redeploy as Windows Service

## Support & Documentation

- [.NET 10 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Worker Services in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/workers)
- [Azure Storage Queues](https://docs.microsoft.com/en-us/azure/storage/queues/)
- [Azure Container Apps](https://docs.microsoft.com/en-us/azure/container-apps/)

## Next Steps

1. Set up Azure Storage account for production
2. Configure production connection strings
3. Set up monitoring and alerting
4. Plan deployment to target environment (Azure App Service, AKS, Container Apps)
5. Update CI/CD pipelines for .NET 10
