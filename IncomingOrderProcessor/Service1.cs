using System;
using System.Messaging;
using System.ServiceProcess;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace IncomingOrderProcessor
{
    public partial class Service1 : ServiceBase
    {
        private MessageQueue orderQueue;
        private string queuePath;
        private IConfiguration configuration;

        public Service1()
        {
            InitializeComponent();
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            configuration = builder.Build();
            queuePath = configuration["MessageQueue:QueuePath"] ?? @".\Private$\productcatalogorders";
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                if (!MessageQueue.Exists(queuePath))
                {
                    orderQueue = MessageQueue.Create(queuePath);
                    LogMessage("Created new queue: " + queuePath);
                }
                else
                {
                    orderQueue = new MessageQueue(queuePath);
                }

                orderQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(Order) });
                
                orderQueue.ReceiveCompleted += new ReceiveCompletedEventHandler(OnOrderReceived);
                orderQueue.BeginReceive();

                LogMessage("Order processing service started successfully. Watching queue: " + queuePath);
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
                if (orderQueue != null)
                {
                    orderQueue.ReceiveCompleted -= OnOrderReceived;
                    orderQueue.Close();
                    orderQueue.Dispose();
                }
                LogMessage("Order processing service stopped.");
            }
            catch (Exception ex)
            {
                LogMessage("Error stopping service: " + ex.Message);
            }
        }

        private void OnOrderReceived(object sender, ReceiveCompletedEventArgs e)
        {
            try
            {
                MessageQueue queue = (MessageQueue)sender;
                Message message = queue.EndReceive(e.AsyncResult);

                Order order = (Order)message.Body;
                
                WriteOrderToConsole(order);
                
                LogMessage($"Order {order.OrderId} processed successfully and removed from queue.");
                
                queue.BeginReceive();
            }
            catch (Exception ex)
            {
                LogMessage("Error processing order: " + ex.Message);
                
                if (orderQueue != null)
                {
                    orderQueue.BeginReceive();
                }
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
}
