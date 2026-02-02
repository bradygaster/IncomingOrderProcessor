# Migration Guide: .NET Framework to .NET 10 with Azure Service Bus

## Overview

This document outlines the migration from the legacy .NET Framework 4.8.1 Windows Service using MSMQ to a modern .NET 10 Worker Service using Azure Service Bus.

## What Changed

### 1. Framework Migration
- **Before**: .NET Framework 4.8.1
- **After**: .NET 10
- **Benefits**: 
  - Cross-platform support (Windows, Linux, macOS)
  - Better performance
  - Modern C# language features
  - Simplified project structure

### 2. Messaging Infrastructure
- **Before**: Microsoft Message Queue (MSMQ) - Local queue `.\Private$\productcatalogorders`
- **After**: Azure Service Bus - Cloud-based queue `productcatalogorders`
- **Benefits**:
  - Cloud-native, scalable messaging
  - No infrastructure dependencies
  - Built-in reliability and dead-letter queues
  - Better monitoring and diagnostics

### 3. Service Model
- **Before**: Windows Service (ServiceBase)
- **After**: Worker Service (BackgroundService)
- **Benefits**:
  - Cross-platform deployment
  - Better integration with .NET hosting model
  - Dependency injection support
  - Modern configuration system

### 4. Message Serialization
- **Before**: XML serialization with XmlMessageFormatter
- **After**: JSON serialization with System.Text.Json
- **Benefits**:
  - Better interoperability
  - Smaller message size
  - Faster serialization
  - More flexible

## Files Changed

### Removed Files
- `App.config` - Replaced by appsettings.json
- `ProjectInstaller.cs` / `ProjectInstaller.Designer.cs` - Not needed for Worker Service
- `Service1.Designer.cs` - Not needed
- `Properties/AssemblyInfo.cs` - Not needed in SDK-style projects
- `ProjectInstaller.resx` - Not needed

### Modified Files
- `IncomingOrderProcessor.csproj` - Converted to SDK-style project
- `Service1.cs` - Rewritten as `OrderProcessorWorker` using Azure Service Bus
- `Program.cs` - Simplified to use HostBuilder
- `Order.cs` - Updated to use modern C# patterns

### Added Files
- `appsettings.json` - Configuration file
- `README.md` - Comprehensive documentation
- `MIGRATION.md` - This file

## Breaking Changes

### Configuration
**Before:**
- No external configuration needed (MSMQ was local)

**After:**
- Requires Azure Service Bus connection string in `appsettings.json`:
```json
{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://...servicebus.windows.net/...",
    "QueueName": "productcatalogorders"
  }
}
```

### Message Format
**Before (XML):**
```xml
<Order>
  <OrderId>123</OrderId>
  <OrderDate>2026-02-02T15:30:00</OrderDate>
  ...
</Order>
```

**After (JSON):**
```json
{
  "OrderId": "123",
  "OrderDate": "2026-02-02T15:30:00Z",
  ...
}
```

### Installation
**Before:**
```powershell
installutil.exe IncomingOrderProcessor.exe
```

**After:**
```powershell
New-Service -Name "IncomingOrderProcessor" -BinaryPathName "C:\path\to\IncomingOrderProcessor.exe"
```

Or run as console application:
```bash
dotnet IncomingOrderProcessor.dll
```

## Migration Steps for Existing Deployments

### 1. Drain Existing MSMQ Queue
Before switching to the new version:
```powershell
# Check message count
$queue = [System.Messaging.MessageQueue]::new(".\Private$\productcatalogorders")
$queue.GetAllMessages().Count

# Process all remaining messages with old service
# Ensure queue is empty before proceeding
```

### 2. Set Up Azure Service Bus
```bash
# Create Service Bus namespace and queue
az servicebus namespace create --name myNamespace --resource-group myRG
az servicebus queue create --name productcatalogorders --namespace-name myNamespace --resource-group myRG

# Get connection string
az servicebus namespace authorization-rule keys list \
  --name RootManageSharedAccessKey \
  --namespace-name myNamespace \
  --resource-group myRG \
  --query primaryConnectionString -o tsv
```

### 3. Update Message Senders
If you have applications sending messages to MSMQ, they need to be updated to send to Azure Service Bus with JSON format.

**Before (MSMQ):**
```csharp
using System.Messaging;

var queue = new MessageQueue(@".\Private$\productcatalogorders");
queue.Formatter = new XmlMessageFormatter(new[] { typeof(Order) });
queue.Send(order);
```

**After (Azure Service Bus):**
```csharp
using Azure.Messaging.ServiceBus;
using System.Text.Json;

var client = new ServiceBusClient(connectionString);
var sender = client.CreateSender("productcatalogorders");
var messageBody = JsonSerializer.Serialize(order);
var message = new ServiceBusMessage(messageBody);
await sender.SendMessageAsync(message);
```

### 4. Deploy New Service
1. Stop old Windows Service
2. Uninstall old service: `sc.exe delete IncomingOrderProcessor`
3. Deploy new .NET 10 application
4. Configure connection string in `appsettings.json` or environment variables
5. Install and start new service

### 5. Verify Operation
```bash
# Check service status (Windows)
Get-Service IncomingOrderProcessor

# View logs
# Logs will be in console output or configured logging destination

# Monitor Azure Service Bus
az servicebus queue show --name productcatalogorders --namespace-name myNamespace --resource-group myRG
```

## Testing the Migration

### Local Testing
1. Create Azure Service Bus namespace and queue (or use emulator)
2. Configure connection string in `appsettings.json`
3. Run: `dotnet run`
4. Send test messages to verify processing

### Test Message
```bash
# Using Azure CLI
az servicebus queue send \
  --namespace-name myNamespace \
  --queue-name productcatalogorders \
  --body '{
    "OrderId": "test-123",
    "OrderDate": "2026-02-02T15:30:00Z",
    "CustomerSessionId": "session-123",
    "Items": [
      {
        "ProductId": 1,
        "ProductName": "Test Product",
        "SKU": "TEST-001",
        "Price": 19.99,
        "Quantity": 1,
        "Subtotal": 19.99
      }
    ],
    "Subtotal": 19.99,
    "Tax": 1.60,
    "Shipping": 5.00,
    "Total": 26.59
  }'
```

## Rollback Plan

If issues occur:
1. Stop new service
2. Restore old .NET Framework service
3. Re-enable MSMQ queue
4. Messages in Azure Service Bus will remain for later processing

## Support

For issues during migration, refer to:
- README.md for configuration and deployment details
- Azure Service Bus documentation: https://docs.microsoft.com/azure/service-bus-messaging/
- .NET 10 migration guide: https://docs.microsoft.com/dotnet/core/migration/

## Security Considerations

### Connection String Security
- **Never commit connection strings to source control**
- Use Azure Key Vault for production: https://docs.microsoft.com/azure/key-vault/
- Use Managed Identity when running in Azure
- Rotate keys regularly

### Managed Identity Setup (Recommended for Azure)
```csharp
// Instead of connection string, use Managed Identity:
var client = new ServiceBusClient(
    "myNamespace.servicebus.windows.net",
    new DefaultAzureCredential()
);
```

Grant the managed identity access:
```bash
az role assignment create \
  --role "Azure Service Bus Data Receiver" \
  --assignee <managed-identity-object-id> \
  --scope /subscriptions/{subscription-id}/resourceGroups/{rg}/providers/Microsoft.ServiceBus/namespaces/{namespace}
```

## Conclusion

This migration modernizes the application for cloud-native operations while maintaining the core order processing functionality. The new architecture is more scalable, maintainable, and aligned with modern .NET best practices.
