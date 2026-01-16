# IncomingOrderProcessor Worker Service

A modern .NET Worker Service that processes orders from an MSMQ message queue.

## Overview

This application has been migrated from a .NET Framework Windows Service to a modern .NET 10 Worker Service. It monitors a Message Queue (MSMQ) for incoming orders and processes them as they arrive.

## Features

- **Background Service**: Runs as a long-running background service using .NET's `BackgroundService` pattern
- **Windows Service Support**: Can be installed and run as a Windows Service
- **MSMQ Integration**: Uses Experimental.System.Messaging for MSMQ support on modern .NET
- **Structured Logging**: Uses Microsoft.Extensions.Logging for logging
- **Modern .NET**: Built on .NET 10 with latest patterns and practices

## Prerequisites

- .NET 10 SDK
- Windows OS (required for MSMQ)
- MSMQ feature enabled in Windows

## Building the Application

```bash
dotnet build
```

## Running the Application

### As a Console Application (for development/testing)

```bash
dotnet run
```

### As a Windows Service

1. Publish the application:
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

2. Install the service using `sc`:
```bash
sc create IncomingOrderProcessor binPath="C:\path\to\IncomingOrderProcessor.WorkerService.exe"
```

3. Start the service:
```bash
sc start IncomingOrderProcessor
```

4. Stop the service:
```bash
sc stop IncomingOrderProcessor
```

5. Delete the service (if needed):
```bash
sc delete IncomingOrderProcessor
```

## Configuration

The service monitors the MSMQ queue at: `.\Private$\productcatalogorders`

If the queue doesn't exist, it will be created automatically when the service starts.

## Message Format

The service expects XML-serialized `Order` objects with the following structure:
- OrderId (string)
- OrderDate (DateTime)
- CustomerSessionId (string)
- Items (List of OrderItem)
  - ProductId (int)
  - ProductName (string)
  - SKU (string)
  - Price (decimal)
  - Quantity (int)
  - Subtotal (decimal)
- Subtotal (decimal)
- Tax (decimal)
- Shipping (decimal)
- Total (decimal)

## Migration Notes

This application was migrated from a .NET Framework 4.8.1 Windows Service (ServiceBase) to a modern .NET 10 Worker Service. Key changes include:

1. **Project Type**: Changed from .NET Framework Console Application to .NET Worker Service
2. **Service Base**: Changed from `System.ServiceProcess.ServiceBase` to `Microsoft.Extensions.Hosting.BackgroundService`
3. **Lifetime Management**: Now uses `IHostedService` lifetime management pattern
4. **Dependency Injection**: Built-in DI container support
5. **Configuration**: Uses modern configuration system with appsettings.json
6. **Logging**: Integrated with Microsoft.Extensions.Logging framework
7. **MSMQ**: Uses Experimental.System.Messaging package for .NET compatibility
