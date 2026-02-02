# Incoming Order Processor

A .NET 10 worker service that processes incoming orders from Azure Storage Queues.

## Overview

This application monitors an Azure Storage Queue for incoming orders and processes them. It has been modernized from a .NET Framework 4.8.1 Windows Service to a .NET 10 Worker Service.

## Migration from .NET Framework 4.8.1

### Changes Made

#### 1. Project Structure
- Converted from .NET Framework to .NET 10 SDK-style project
- Removed legacy Windows Service infrastructure
- Adopted modern Worker Service pattern using `BackgroundService`

#### 2. Messaging Infrastructure
- **Before**: Microsoft Message Queuing (MSMQ)
- **After**: Azure Storage Queues
- This change enables cloud-native deployment to Azure services like:
  - Azure App Service
  - Azure Kubernetes Service (AKS)
  - Azure Container Apps
  - Azure App Service Containers

#### 3. Serialization
- **Before**: XML serialization with `XmlMessageFormatter`
- **After**: JSON serialization with `System.Text.Json`

#### 4. Configuration
- **Before**: `App.config`
- **After**: `appsettings.json` with support for environment-specific settings

#### 5. Dependency Injection & Logging
- Implemented using `Microsoft.Extensions.Hosting`
- Modern structured logging with `ILogger<T>`

## Configuration

Configure the application using `appsettings.json`:

```json
{
  "OrderProcessor": {
    "QueueConnectionString": "UseDevelopmentStorage=true",
    "QueueName": "productcatalogorders"
  }
}
```

### Connection String Options

- **Local Development**: `UseDevelopmentStorage=true` (requires Azurite)
- **Azure Storage**: `DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net`

## Running the Application

### Prerequisites

- .NET 10 SDK
- Azure Storage Emulator (Azurite) for local development

### Local Development

1. Start Azurite (Azure Storage Emulator):
   ```bash
   azurite --silent --location c:\azurite --debug c:\azurite\debug.log
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

### Production Deployment

The application can be deployed as:
- A console application
- A Windows Service (using `sc.exe` or Windows Service Management)
- A systemd service on Linux
- A containerized application in Docker/Kubernetes
- An Azure Container App

## Resolved Migration Issues

✅ **Service Control Manager API calls** - Replaced Windows Service with Worker Service  
✅ **MSMQ usage** - Replaced with Azure Storage Queues for cloud compatibility

## Benefits of the Migration

1. **Cross-Platform**: Can run on Windows, Linux, and macOS
2. **Cloud-Ready**: Native integration with Azure services
3. **Modern Architecture**: Leverages latest .NET features and patterns
4. **Better Performance**: Improved async/await patterns throughout
5. **Easier Testing**: Dependency injection enables better testability
6. **Configuration Management**: Flexible configuration with multiple sources
7. **Observability**: Structured logging and diagnostics support
