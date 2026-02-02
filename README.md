# IncomingOrderProcessor - .NET 10 with Azure Service Bus

This application has been modernized from .NET Framework 4.8.1 to .NET 10 and migrated from MSMQ to Azure Service Bus.

## Overview

The IncomingOrderProcessor is a background service that processes incoming orders from an Azure Service Bus queue. It receives order messages, deserializes them, and displays formatted order information to the console.

## Prerequisites

- .NET 10.0 SDK or later
- Azure Service Bus namespace with a queue configured

## Configuration

The application requires an Azure Service Bus connection string to be configured. You can provide this in several ways:

### 1. appsettings.json

Edit `appsettings.json` and add your connection string:

```json
{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key",
    "QueueName": "productcatalogorders"
  }
}
```

### 2. User Secrets (Development)

For local development, use user secrets to keep connection strings secure:

```bash
dotnet user-secrets init
dotnet user-secrets set "ServiceBus:ConnectionString" "your-connection-string-here"
```

### 3. Environment Variables (Production)

For production deployments, use environment variables:

```bash
export ServiceBus__ConnectionString="your-connection-string-here"
export ServiceBus__QueueName="productcatalogorders"
```

## Running the Application

### Local Development

```bash
dotnet run
```

### Build and Run

```bash
dotnet build
dotnet run --project IncomingOrderProcessor.csproj
```

## Deployment Options

This .NET 10 Worker Service can be deployed to multiple Azure platforms:

- **Azure App Service**: Deploy as a background worker
- **Azure Container Apps**: Deploy as a containerized background job
- **Azure Kubernetes Service (AKS)**: Deploy as a Kubernetes deployment
- **Azure Virtual Machines**: Run as a systemd service on Linux or Windows Service

## Message Format

The service expects messages in JSON format with the following structure:

```json
{
  "OrderId": "guid",
  "OrderDate": "2024-01-01T00:00:00Z",
  "CustomerSessionId": "session-id",
  "Items": [
    {
      "ProductId": 1,
      "ProductName": "Product Name",
      "SKU": "SKU-123",
      "Price": 99.99,
      "Quantity": 2,
      "Subtotal": 199.98
    }
  ],
  "Subtotal": 199.98,
  "Tax": 16.00,
  "Shipping": 10.00,
  "Total": 225.98
}
```

## Migration Summary

### Changes Made

1. **Framework Upgrade**: Migrated from .NET Framework 4.8.1 to .NET 10
2. **Project Format**: Converted from old-style .csproj to SDK-style project
3. **Service Model**: Migrated from Windows Service (ServiceBase) to BackgroundService
4. **Hosting**: Migrated to modern .NET Generic Host
5. **Messaging**: Replaced MSMQ (System.Messaging) with Azure Service Bus (Azure.Messaging.ServiceBus)
6. **Serialization**: Changed from XmlMessageFormatter to System.Text.Json
7. **Async/Await**: Updated to use async patterns throughout
8. **Logging**: Integrated ILogger for structured logging
9. **Configuration**: Using IConfiguration with appsettings.json, user secrets, and environment variables

### Removed Components

- Windows Service installer (ProjectInstaller.cs)
- MSMQ dependencies (System.Messaging)
- AssemblyInfo.cs (handled by SDK-style project)
- App.config (replaced by appsettings.json)

## Troubleshooting

### Connection String Not Configured

If you see the error "Azure Service Bus connection string is not configured", ensure you have set the connection string using one of the methods described in the Configuration section.

### Queue Not Found

If the queue doesn't exist in your Service Bus namespace, create it using:
- Azure Portal
- Azure CLI: `az servicebus queue create --resource-group <rg> --namespace-name <namespace> --name productcatalogorders`
- Terraform or ARM templates

## Support

For issues or questions, please refer to:
- [Azure Service Bus Documentation](https://learn.microsoft.com/azure/service-bus-messaging/)
- [.NET 10 Documentation](https://learn.microsoft.com/dotnet/)
