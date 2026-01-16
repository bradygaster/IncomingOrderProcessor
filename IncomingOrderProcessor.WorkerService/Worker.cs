using Experimental.System.Messaging;
using System.Text;

namespace IncomingOrderProcessor.WorkerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private MessageQueue? _orderQueue;
    private const string QueuePath = @".\Private$\productcatalogorders";

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!MessageQueue.Exists(QueuePath))
            {
                _orderQueue = MessageQueue.Create(QueuePath);
                LogMessage("Created new queue: " + QueuePath);
            }
            else
            {
                _orderQueue = new MessageQueue(QueuePath);
            }

            _orderQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(Order) });
            
            LogMessage("Order processing service started successfully. Watching queue: " + QueuePath);

            // Process messages in a loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Use a short timeout to allow for cancellation checking
                    var message = _orderQueue.Receive(TimeSpan.FromSeconds(1));
                    
                    if (message.Body is Order order)
                    {
                        WriteOrderToConsole(order);
                        LogMessage($"Order {order.OrderId} processed successfully and removed from queue.");
                    }
                }
                catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    // Timeout is expected, continue to next iteration
                    await Task.Delay(100, stoppingToken);
                }
                catch (Exception ex)
                {
                    LogMessage("Error processing order: " + ex.Message);
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage("Error starting service: " + ex.Message);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_orderQueue != null)
            {
                _orderQueue.Close();
                _orderQueue.Dispose();
            }
            LogMessage("Order processing service stopped.");
        }
        catch (Exception ex)
        {
            LogMessage("Error stopping service: " + ex.Message);
        }

        await base.StopAsync(cancellationToken);
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
        _logger.LogInformation(logMessage);
    }
}
