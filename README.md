# Incoming Order Processor - Azure Service Bus Migration

This Windows Service processes incoming orders from Azure Service Bus (migrated from MSMQ).

## Architecture

The service uses:
- **Azure Service Bus** for message queue management
- **JSON serialization** for message format (replaced XML/Binary)
- **Async message processing** with ServiceBusProcessor

## Prerequisites

- .NET Framework 4.8.1
- Azure subscription
- Azure Service Bus namespace

## Configuration

### 1. Deploy Azure Service Bus Infrastructure

Deploy the Service Bus namespace and queue using the provided Bicep template:

```bash
# Login to Azure
az login

# Create resource group (if not exists)
az group create --name rg-orderprocessor --location eastus

# Deploy Service Bus resources
az deployment group create \
  --resource-group rg-orderprocessor \
  --template-file infrastructure/service-bus.bicep \
  --parameters serviceBusSku=Standard
```

### 2. Get Connection String

After deployment, retrieve the connection string:

```bash
az servicebus namespace authorization-rule keys list \
  --resource-group rg-orderprocessor \
  --namespace-name <your-namespace-name> \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString \
  --output tsv
```

### 3. Configure the Application

Update `appsettings.json` with your Service Bus connection string:

```json
{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=<key>",
    "QueueName": "productcatalogorders"
  }
}
```

**Security Best Practices:**
- Store connection strings in Azure Key Vault
- Use Managed Identity for authentication in production
- Never commit connection strings to source control

### 4. Using Azure Key Vault (Recommended)

For production, use Key Vault to store the connection string:

```bash
# Create Key Vault
az keyvault create --name kv-orderprocessor --resource-group rg-orderprocessor --location eastus

# Store connection string
az keyvault secret set \
  --vault-name kv-orderprocessor \
  --name ServiceBusConnectionString \
  --value "<your-connection-string>"

# Grant service access (if using Managed Identity)
az keyvault set-policy \
  --name kv-orderprocessor \
  --object-id <service-principal-id> \
  --secret-permissions get list
```

## Building and Running

### Build the Service

```bash
# Using Visual Studio
msbuild IncomingOrderProcessor.sln /p:Configuration=Release

# Or open in Visual Studio and build
```

### Install the Windows Service

```bash
# Run as Administrator
sc create IncomingOrderProcessor binPath= "C:\path\to\IncomingOrderProcessor.exe"
sc start IncomingOrderProcessor
```

## Message Format

Orders are sent as JSON messages to the Service Bus queue:

```json
{
  "OrderId": "guid",
  "OrderDate": "2026-01-16T10:30:00",
  "CustomerSessionId": "session-guid",
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
  "Tax": 15.00,
  "Shipping": 5.00,
  "Total": 219.98
}
```

## Sending Test Messages

### Using Azure Portal
1. Navigate to your Service Bus namespace
2. Select the `productcatalogorders` queue
3. Click "Service Bus Explorer"
4. Send a message with the JSON format above

### Using Azure CLI

```bash
az servicebus queue message send \
  --resource-group rg-orderprocessor \
  --namespace-name <namespace-name> \
  --queue-name productcatalogorders \
  --body '{"OrderId":"test-123","OrderDate":"2026-01-16T10:00:00","CustomerSessionId":"test-session","Items":[],"Subtotal":100.00,"Tax":8.00,"Shipping":5.00,"Total":113.00}'
```

### Using C# Producer Code

```csharp
using Azure.Messaging.ServiceBus;
using System.Text.Json;

var connectionString = "<your-connection-string>";
var queueName = "productcatalogorders";

await using var client = new ServiceBusClient(connectionString);
var sender = client.CreateSender(queueName);

var order = new Order
{
    OrderId = Guid.NewGuid().ToString(),
    OrderDate = DateTime.Now,
    CustomerSessionId = Guid.NewGuid().ToString(),
    // ... other properties
};

var messageBody = JsonSerializer.Serialize(order);
var message = new ServiceBusMessage(messageBody);

await sender.SendMessageAsync(message);
```

## Migration from MSMQ

### Key Changes

| Aspect | MSMQ | Azure Service Bus |
|--------|------|-------------------|
| Queue Path | `.\Private$\queuename` | Connection string + queue name |
| Serialization | XML/Binary | JSON |
| API | Synchronous | Asynchronous |
| Message Handling | BeginReceive/EndReceive | ServiceBusProcessor |
| Error Handling | Try-catch with retry | ProcessErrorAsync handler |

### Breaking Changes

1. **Serialization**: Messages must be JSON-formatted
2. **Connection**: Requires Service Bus connection string
3. **Platform**: Requires Azure subscription and Service Bus namespace

## Monitoring

Monitor your Service Bus queue in Azure Portal:
- **Active Messages**: Messages waiting to be processed
- **Dead-letter Messages**: Messages that failed processing after max retries
- **Metrics**: Throughput, latency, errors

## Troubleshooting

### Service Won't Start

1. Check `appsettings.json` exists in the service directory
2. Verify connection string is valid
3. Ensure Service Bus namespace and queue exist
4. Check Windows Event Log for error details

### Messages Not Processing

1. Verify queue name matches configuration
2. Check Service Bus namespace is accessible
3. Review message format (must be valid JSON)
4. Check Service Bus metrics for errors

### Connection Errors

1. Verify connection string format
2. Check firewall/network rules
3. Ensure Service Bus namespace is active
4. Verify access keys are not expired

## License

[Your License Here]
