using Azure.Messaging.ServiceBus;
using System.Text;
using System.Text.Json;

namespace IncomingOrderProcessor;

public class OrderProcessorWorker : BackgroundService
{
    private readonly ILogger<OrderProcessorWorker> _logger;
    private readonly IConfiguration _configuration;
    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;
    private readonly string _queueName;

    public OrderProcessorWorker(ILogger<OrderProcessorWorker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _queueName = _configuration["ServiceBus:QueueName"] ?? "productcatalogorders";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var connectionString = _configuration["ServiceBus:ConnectionString"];
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Azure Service Bus connection string is not configured. Please set ServiceBus:ConnectionString in appsettings.json or environment variables.");
                return;
            }

            _client = new ServiceBusClient(connectionString);
            _processor = _client.CreateProcessor(_queueName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

            _processor.ProcessMessageAsync += MessageHandler;
            _processor.ProcessErrorAsync += ErrorHandler;

            await _processor.StartProcessingAsync(stoppingToken);
            
            _logger.LogInformation("Order processing service started successfully. Watching queue: {QueueName}", _queueName);

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting service: {Message}", ex.Message);
            throw;
        }
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        try
        {
            var body = args.Message.Body.ToString();
            var order = JsonSerializer.Deserialize<Order>(body);

            if (order != null)
            {
                WriteOrderToConsole(order);
                _logger.LogInformation("Order {OrderId} processed successfully and removed from queue.", order.OrderId);
            }

            // Complete the message
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order: {Message}", ex.Message);
            // The message will be automatically retried or moved to dead letter queue based on configuration
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error occurred while processing message from {EntityPath}: {Message}", 
            args.EntityPath, args.Exception.Message);
        return Task.CompletedTask;
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

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (_processor != null)
            {
                await _processor.StopProcessingAsync(stoppingToken);
                await _processor.DisposeAsync();
            }

            if (_client != null)
            {
                await _client.DisposeAsync();
            }

            _logger.LogInformation("Order processing service stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping service: {Message}", ex.Message);
        }

        await base.StopAsync(stoppingToken);
    }
}
