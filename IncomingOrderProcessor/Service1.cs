using System;
using Azure.Messaging.ServiceBus;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace IncomingOrderProcessor
{
    public partial class Service1 : ServiceBase
    {
        private ServiceBusClient serviceBusClient;
        private ServiceBusProcessor processor;
        private string connectionString;
        private string queueName;
        private Task processingTask;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                LoadConfiguration();

                serviceBusClient = new ServiceBusClient(connectionString);
                processor = serviceBusClient.CreateProcessor(queueName, new ServiceBusProcessorOptions
                {
                    MaxConcurrentCalls = 1,
                    AutoCompleteMessages = false
                });

                processor.ProcessMessageAsync += ProcessMessageHandler;
                processor.ProcessErrorAsync += ProcessErrorHandler;

                // Start processing in a background task to avoid blocking OnStart
                processingTask = Task.Run(async () =>
                {
                    try
                    {
                        await processor.StartProcessingAsync();
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error in processing task: {ex.Message}");
                        throw;
                    }
                });

                LogMessage($"Order processing service started successfully. Watching queue: {queueName}");
            }
            catch (Exception ex)
            {
                LogMessage("Error starting service: " + ex.Message);
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                if (processor != null)
                {
                    // Using Task.Run with timeout is the recommended pattern for Windows Services
                    // to avoid deadlocks when disposing async resources in synchronous OnStop
                    Task.Run(async () =>
                    {
                        await processor.StopProcessingAsync();
                        await processor.DisposeAsync();
                    }).Wait(TimeSpan.FromSeconds(30));
                }

                if (serviceBusClient != null)
                {
                    Task.Run(async () =>
                    {
                        await serviceBusClient.DisposeAsync();
                    }).Wait(TimeSpan.FromSeconds(10));
                }

                LogMessage("Order processing service stopped.");
            }
            catch (Exception ex)
            {
                LogMessage("Error stopping service: " + ex.Message);
            }
        }

        private async Task ProcessMessageHandler(ProcessMessageEventArgs args)
        {
            try
            {
                string body = args.Message.Body.ToString();
                Order order = JsonSerializer.Deserialize<Order>(body);

                WriteOrderToConsole(order);

                await args.CompleteMessageAsync(args.Message);

                LogMessage($"Order {order.OrderId} processed successfully and removed from queue.");
            }
            catch (Exception ex)
            {
                LogMessage("Error processing order: " + ex.Message);
                // Don't complete the message on error - it will be retried
            }
        }

        private Task ProcessErrorHandler(ProcessErrorEventArgs args)
        {
            LogMessage($"Error in message processing: {args.Exception.Message}");
            LogMessage($"Error Source: {args.ErrorSource}");
            LogMessage($"Entity Path: {args.EntityPath}");
            return Task.CompletedTask;
        }

        private void LoadConfiguration()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (!File.Exists(configPath))
                {
                    throw new FileNotFoundException("Configuration file not found: appsettings.json");
                }

                string json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<ConfigurationRoot>(json);

                if (config == null || config.ServiceBus == null)
                {
                    throw new InvalidOperationException("Invalid configuration: ServiceBus section is missing");
                }

                connectionString = config.ServiceBus.ConnectionString;
                queueName = config.ServiceBus.QueueName;

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException("ServiceBus:ConnectionString is not configured");
                }

                if (string.IsNullOrWhiteSpace(queueName))
                {
                    throw new InvalidOperationException("ServiceBus:QueueName is not configured");
                }
            }
            catch (Exception ex)
            {
                LogMessage("Error loading configuration: " + ex.Message);
                throw;
            }
        }

        private void WriteOrderToConsole(Order order)
        {
            var output = new StringBuilder();
            output.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            output.AppendLine("║                      NEW ORDER RECEIVED                        ║");
            output.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            output.AppendLine();
            output.AppendLine($"  Order ID:            {order.OrderId}");
            output.AppendLine($"  Order Date:          {order.OrderDate:yyyy-MM-dd HH:mm:ss}");
            output.AppendLine($"  Customer Session:    {order.CustomerSessionId}");
            output.AppendLine();
            output.AppendLine("  ┌──────────────────────────────────────────────────────────────┐");
            output.AppendLine("  │ ORDER ITEMS                                                  │");
            output.AppendLine("  └──────────────────────────────────────────────────────────────┘");
            
            if (order.Items != null && order.Items.Count > 0)
            {
                foreach (var item in order.Items)
                {
                    output.AppendLine($"    • {item.ProductName}");
                    output.AppendLine($"      SKU: {item.SKU} | Product ID: {item.ProductId}");
                    output.AppendLine($"      Quantity: {item.Quantity} × ${item.Price:F2} = ${item.Subtotal:F2}");
                    output.AppendLine();
                }
            }
            else
            {
                output.AppendLine("    (No items)");
                output.AppendLine();
            }
            
            output.AppendLine("  ┌──────────────────────────────────────────────────────────────┐");
            output.AppendLine("  │ ORDER SUMMARY                                                │");
            output.AppendLine("  └──────────────────────────────────────────────────────────────┘");
            output.AppendLine($"    Subtotal:          ${order.Subtotal:F2}");
            output.AppendLine($"    Tax:               ${order.Tax:F2}");
            output.AppendLine($"    Shipping:          ${order.Shipping:F2}");
            output.AppendLine($"    ───────────────────────────────────");
            output.AppendLine($"    TOTAL:             ${order.Total:F2}");
            output.AppendLine();
            output.AppendLine("════════════════════════════════════════════════════════════════");
            
            Console.WriteLine(output.ToString());
        }

        private void LogMessage(string message)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            Console.WriteLine(logMessage);
        }
    }

    public class ConfigurationRoot
    {
        public ServiceBusConfiguration ServiceBus { get; set; }
    }

    public class ServiceBusConfiguration
    {
        public string ConnectionString { get; set; }
        public string QueueName { get; set; }
    }
}
