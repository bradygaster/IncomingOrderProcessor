using Azure.Messaging.ServiceBus;
using System.Text;
using System.Text.Json;

namespace IncomingOrderProcessor;

public class OrderProcessorWorker : BackgroundService
{
    private readonly ILogger<OrderProcessorWorker> _logger;
    private readonly IConfiguration _configuration;
    private ServiceBusProcessor? _processor;
    private ServiceBusClient? _client;

    public OrderProcessorWorker(ILogger<OrderProcessorWorker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var connectionString = _configuration["ServiceBus:ConnectionString"];
            var queueName = _configuration["ServiceBus:QueueName"] ?? "productcatalogorders";

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("ServiceBus:ConnectionString is not configured. Please set it in appsettings.json or environment variables.");
                return;
            }

            _client = new ServiceBusClient(connectionString);
            _processor = _client.CreateProcessor(queueName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

            _processor.ProcessMessageAsync += MessageHandler;
            _processor.ProcessErrorAsync += ErrorHandler;

            await _processor.StartProcessingAsync(stoppingToken);
            _logger.LogInformation("Order processing service started successfully. Watching queue: {QueueName}", queueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Order processing service is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting service");
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

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order");
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error in message processing: {ErrorSource}", args.ErrorSource);
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

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order processing service is stopping.");

        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
