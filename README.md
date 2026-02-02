# Incoming Order Processor

A modernized .NET 10 worker service that processes incoming orders from Azure Service Bus.

## Overview

This application has been modernized from:
- **.NET Framework 4.8.1** → **.NET 10**
- **MSMQ** → **Azure Service Bus**
- **Windows Service** → **Worker Service (cross-platform)**

## Prerequisites

- .NET 10 SDK or later
- Azure Service Bus namespace and queue
- Azure subscription (for Service Bus)

## Configuration

The application uses `appsettings.json` for configuration. You need to configure your Azure Service Bus connection string:

```json
{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key",
    "QueueName": "productcatalogorders"
  }
}
```

### Configuration Options

1. **appsettings.json** (recommended for development)
2. **Environment Variables**:
   - `ServiceBus__ConnectionString`
   - `ServiceBus__QueueName`
3. **Azure Key Vault** (recommended for production)

## Azure Service Bus Setup

### Create Azure Service Bus Queue

Using Azure CLI:

```bash
# Create a resource group (if needed)
az group create --name myResourceGroup --location eastus

# Create a Service Bus namespace
az servicebus namespace create --resource-group myResourceGroup \
  --name myServiceBusNamespace --location eastus

# Create a queue
az servicebus queue create --resource-group myResourceGroup \
  --namespace-name myServiceBusNamespace --name productcatalogorders

# Get the connection string
az servicebus namespace authorization-rule keys list \
  --resource-group myResourceGroup \
  --namespace-name myServiceBusNamespace \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString --output tsv
```

Using Azure Portal:

1. Create a Service Bus namespace
2. Create a queue named `productcatalogorders`
3. Go to "Shared access policies" and copy the connection string

## Running the Application

### Development (Console)

```bash
dotnet run
```

### Production - Windows Service

Install as a Windows Service using `sc.exe` or PowerShell:

```powershell
# Publish the application
dotnet publish -c Release -o ./publish

# Create Windows Service
New-Service -Name "IncomingOrderProcessor" `
  -BinaryPathName "C:\path\to\publish\IncomingOrderProcessor.exe" `
  -DisplayName "Incoming Order Processor" `
  -Description "Receives orders from Azure Service Bus and processes them" `
  -StartupType Automatic

# Start the service
Start-Service -Name "IncomingOrderProcessor"
```

### Production - Linux Systemd

Create a systemd service file `/etc/systemd/system/incoming-order-processor.service`:

```ini
[Unit]
Description=Incoming Order Processor
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /opt/IncomingOrderProcessor/IncomingOrderProcessor.dll
Restart=on-failure
RestartSec=10
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ServiceBus__ConnectionString=your-connection-string

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable incoming-order-processor
sudo systemctl start incoming-order-processor
sudo systemctl status incoming-order-processor
```

### Docker

Create a `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["IncomingOrderProcessor/IncomingOrderProcessor.csproj", "IncomingOrderProcessor/"]
RUN dotnet restore "IncomingOrderProcessor/IncomingOrderProcessor.csproj"
COPY . .
WORKDIR "/src/IncomingOrderProcessor"
RUN dotnet build "IncomingOrderProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IncomingOrderProcessor.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IncomingOrderProcessor.dll"]
```

Build and run:

```bash
docker build -t incoming-order-processor .
docker run -d --name order-processor \
  -e ServiceBus__ConnectionString="your-connection-string" \
  incoming-order-processor
```

## Azure Deployment Options

### Azure Container Apps

```bash
az containerapp create \
  --name incoming-order-processor \
  --resource-group myResourceGroup \
  --environment myEnvironment \
  --image myregistry.azurecr.io/incoming-order-processor:latest \
  --secrets servicebus-connection-string="your-connection-string" \
  --env-vars ServiceBus__ConnectionString=secretref:servicebus-connection-string \
  --cpu 0.5 --memory 1.0Gi
```

### Azure Kubernetes Service (AKS)

Deploy using Kubernetes manifests with secrets for the connection string.

### Azure App Service

Deploy as a Web Job or using Azure App Service for containers.

## Message Format

The application expects JSON messages in the following format:

```json
{
  "OrderId": "550e8400-e29b-41d4-a716-446655440000",
  "OrderDate": "2026-02-02T15:30:00Z",
  "CustomerSessionId": "session-123",
  "Items": [
    {
      "ProductId": 1,
      "ProductName": "Product Name",
      "SKU": "SKU-001",
      "Price": 19.99,
      "Quantity": 2,
      "Subtotal": 39.98
    }
  ],
  "Subtotal": 39.98,
  "Tax": 3.20,
  "Shipping": 5.00,
  "Total": 48.18
}
```

## Migration Notes

### Breaking Changes from Previous Version

1. **Queue Location**: Changed from MSMQ local queue (`.\Private$\productcatalogorders`) to Azure Service Bus
2. **Message Format**: Changed from XML serialization to JSON
3. **Deployment**: No longer requires MSMQ feature installation on Windows
4. **Cross-platform**: Can now run on Windows, Linux, and macOS

### Compatibility

If you have existing MSMQ messages, you'll need to:
1. Drain existing MSMQ queue before switching
2. Convert message format from XML to JSON
3. Send messages to Azure Service Bus

## Troubleshooting

### Connection Issues

- Verify the Service Bus connection string is correct
- Check firewall rules allow outbound connections to `*.servicebus.windows.net`
- Ensure the queue name matches exactly

### Message Processing Errors

- Check logs for detailed error messages
- Verify message format matches expected JSON schema
- Check Azure Service Bus metrics for dead-letter queue messages

### Performance Tuning

Adjust concurrent message processing in `OrderProcessorWorker.cs`:

```csharp
_processor = _client.CreateProcessor(_queueName, new ServiceBusProcessorOptions
{
    AutoCompleteMessages = false,
    MaxConcurrentCalls = 5  // Increase for higher throughput
});
```

## Monitoring

- Use Azure Monitor to track Service Bus metrics
- Configure Application Insights for detailed telemetry
- Check worker service logs for processing status

## Security Best Practices

1. **Never commit connection strings** to source control
2. Use **Azure Key Vault** for production secrets
3. Use **Managed Identity** when running in Azure
4. Rotate access keys regularly
5. Use **least privilege** SAS policies

## License

[Your License Here]

## Support

For issues and questions, please open an issue on the GitHub repository.
