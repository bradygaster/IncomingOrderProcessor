# Docker Support for IncomingOrderProcessor

## Overview

This repository contains Docker configuration for the IncomingOrderProcessor application.

## Important Notes

### Windows Containers Required

This application is built on .NET Framework 4.8.1 and uses Windows-specific technologies:
- **Windows Service**: The application is designed to run as a Windows Service
- **MSMQ**: Uses Microsoft Message Queuing for order processing

Due to these dependencies, **Windows containers are required**. The Dockerfile uses Windows Server Core base images.

### Prerequisites

- Docker Desktop for Windows with Windows containers enabled
- Windows Server containers support
- At least 4GB of RAM allocated to Docker

### Switching to Windows Containers

If you're using Docker Desktop:
1. Right-click the Docker icon in the system tray
2. Select "Switch to Windows containers..."
3. Confirm the switch

## Building the Container

```bash
docker build -t incoming-order-processor:latest .
```

## Running with Docker Compose

```bash
# Start the service
docker-compose up -d

# View logs
docker-compose logs -f incoming-order-processor

# Stop the service
docker-compose down
```

## Running the Container Directly

```bash
docker run -d --name incoming-order-processor incoming-order-processor:latest
```

## Current Limitations

1. **Console Mode**: The application currently only runs as a Windows Service. To run properly in a container, the application would need to be modified to support console mode execution. This would require changes to `Program.cs` to detect the environment and run the service logic directly when not in service mode.

2. **MSMQ Configuration**: MSMQ is enabled in the container, but queue creation and management may require additional configuration depending on your specific use case.

## Migration Path to .NET 8.0

To use the Linux-based .NET 8.0 images mentioned in the modernization plan, the application would need to be migrated:
- Port from .NET Framework 4.8.1 to .NET 8.0
- Replace MSMQ with a cross-platform message queue (e.g., RabbitMQ, Azure Service Bus, or Redis)
- Remove Windows Service dependencies in favor of hosted services or worker services
- Update the Dockerfile to use `mcr.microsoft.com/dotnet/sdk:8.0` and `mcr.microsoft.com/dotnet/runtime:8.0`

## Docker Compose Configuration

The `docker-compose.yml` includes:
- Service definition for the order processor
- Network configuration for inter-service communication
- Volume for MSMQ data persistence

The `docker-compose.override.yml` provides local development overrides:
- Development environment variables
- Resource limits appropriate for local development
