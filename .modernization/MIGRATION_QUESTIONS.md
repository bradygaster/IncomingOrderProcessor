# 🎯 Migration Preferences Needed

## Patterns Detected in Your Repository

I've analyzed the `IncomingOrderProcessor` repository and found the following legacy patterns that need migration decisions:

### Summary of Findings:
- **Windows Service** (1 service found - Service1.cs)
  - Currently uses `System.ServiceProcess.ServiceBase`
  - Runs as a Windows background service
  
- **MSMQ (Microsoft Message Queue)** 
  - Uses `System.Messaging` for queue-based message processing
  - Queue path: `.\Private$\productcatalogorders`
  
- **.NET Framework 4.8.1**
  - Current target framework that needs modernization

### Migration Decisions Needed:

Please reply with your choices for the following:

1. **Windows Service → ?** 
   - Options: **Worker Service** (recommended for containers) / Keep as Console App / SystemD Service
   
2. **MSMQ → ?**
   - Options: **Azure Service Bus** / Azure Storage Queues / RabbitMQ / Keep MSMQ (not container-friendly)
   
3. **Target Framework:**
   - Options: **.NET 10** / .NET 9 / .NET 8
   
4. **Azure Compute Target:**
   - Options: **Azure Container Apps** / Azure Kubernetes Service (AKS) / Azure App Service

---

## How to Respond

Reply to the issue with your choices like:

```
@copilot Worker Service, Azure Service Bus, .NET 10, Container Apps
```

Or simply say:

```
@copilot defaults
```

This will use Microsoft's recommended options:
- Windows Service → **Worker Service** (BackgroundService pattern)
- MSMQ → **Azure Service Bus** (cloud-native messaging)
- Target Framework → **.NET 10**
- Azure Compute → **Azure Container Apps**

---

**Note:** This assessment is paused waiting for your migration preferences. Once you reply, I will create the playbook and complete the full assessment.
