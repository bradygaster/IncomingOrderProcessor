using Azure.Messaging.ServiceBus;
using System.Text;
using System.Text.Json;

namespace IncomingOrderProcessor;

public class OrderProcessorService : BackgroundService
{
    private ServiceBusProcessor? _processor;
    private ServiceBusClient? _client;
    private readonly ILogger<OrderProcessorService> _logger;
    private readonly IConfiguration _configuration;
    private const string DefaultQueueName = "productcatalogorders";

    public OrderProcessorService(ILogger<OrderProcessorService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var connectionString = _configuration["ServiceBus:ConnectionString"];
            var queueName = _configuration["ServiceBus:QueueName"] ?? DefaultQueueName;

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Azure Service Bus connection string is not configured. Please set ServiceBus:ConnectionString in configuration.");
                return;
            }

            _client = new ServiceBusClient(connectionString);
            _processor = _client.CreateProcessor(queueName, new ServiceBusProcessorOptions());

            _processor.ProcessMessageAsync += ProcessMessageAsync;
            _processor.ProcessErrorAsync += ProcessErrorAsync;

            await _processor.StartProcessingAsync(stoppingToken);
            _logger.LogInformation("Order processing service started successfully. Watching queue: {QueueName}", queueName);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Order processing service is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting service: {Message}", ex.Message);
            throw;
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var messageBody = args.Message.Body.ToString();
            Order? order = null;

            try
            {
                order = JsonSerializer.Deserialize<Order>(messageBody);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to deserialize message as Order. Message body: {MessageBody}", messageBody);
                // Dead-letter the message as it cannot be processed
                await args.DeadLetterMessageAsync(args.Message, "DeserializationError", "Message body is not valid Order JSON");
                return;
            }

            if (order != null)
            {
                WriteOrderToConsole(order);
                _logger.LogInformation("Order {OrderId} processed successfully and removed from queue.", order.OrderId);
            }

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order: {Message}", ex.Message);
            // Message will be retried or moved to dead-letter queue based on Service Bus configuration
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error in Service Bus processor: {Message}", args.Exception.Message);
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
