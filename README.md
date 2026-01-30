# Incoming Order Processor

A modernized order processing service built with .NET 10 and Azure Service Bus, designed to run in Azure Container Apps.

## Overview

This application is a background worker service that:
- Receives order messages from Azure Service Bus
- Processes and displays order information
- Runs as a containerized application in Azure Container Apps

## Migration from Legacy Version

This application has been modernized from:
- **.NET Framework 4.8.1** → **.NET 10**
- **MSMQ** → **Azure Service Bus**
- **Windows Service** → **Worker Service (containerized)**

## Configuration

The application requires the following configuration:

### appsettings.json

```json
{
  "ServiceBus": {
    "ConnectionString": "<your-service-bus-connection-string>",
    "QueueName": "productcatalogorders"
  }
}
```

### Environment Variables

You can also configure the application using environment variables:

- `ServiceBus__ConnectionString`: Azure Service Bus connection string
- `ServiceBus__QueueName`: Name of the queue (default: "productcatalogorders")

## Running Locally

### Prerequisites

- .NET 10 SDK
- Azure Service Bus namespace with a queue named "productcatalogorders"

### Steps

1. Update `appsettings.json` or set environment variables with your Service Bus connection string
2. Run the application:

```bash
dotnet run --project IncomingOrderProcessor/IncomingOrderProcessor.csproj
```

## Running with Docker

### Build the Docker image

```bash
docker build -t incoming-order-processor:latest .
```

### Run the container

```bash
docker run -e ServiceBus__ConnectionString="<your-connection-string>" incoming-order-processor:latest
```

## Deploying to Azure Container Apps

### Prerequisites

- Azure CLI installed
- Azure subscription
- Azure Service Bus namespace with a queue

### Steps

1. **Create a resource group** (if you don't have one):

```bash
az group create --name myResourceGroup --location eastus
```

2. **Create an Azure Container Registry** (if you don't have one):

```bash
az acr create --resource-group myResourceGroup --name myregistry --sku Basic
```

3. **Build and push the image**:

```bash
az acr build --registry myregistry --image incoming-order-processor:latest .
```

4. **Create a Container Apps environment**:

```bash
az containerapp env create \
  --name myEnvironment \
  --resource-group myResourceGroup \
  --location eastus
```

5. **Deploy the container app**:

```bash
az containerapp create \
  --name incoming-order-processor \
  --resource-group myResourceGroup \
  --environment myEnvironment \
  --image myregistry.azurecr.io/incoming-order-processor:latest \
  --registry-server myregistry.azurecr.io \
  --secrets service-bus-connection-string="<your-service-bus-connection-string>" \
  --env-vars ServiceBus__ConnectionString=secretref:service-bus-connection-string \
  --cpu 0.25 --memory 0.5Gi
```

## Message Format

The application expects JSON messages in the following format:

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
      "Price": 10.00,
      "Quantity": 2,
      "Subtotal": 20.00
    }
  ],
  "Subtotal": 20.00,
  "Tax": 2.00,
  "Shipping": 5.00,
  "Total": 27.00
}
```

## Development

### Building the project

```bash
dotnet build
```

### Publishing the project

```bash
dotnet publish -c Release
```

## License

MIT
