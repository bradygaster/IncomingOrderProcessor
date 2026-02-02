using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace IncomingOrderProcessor;

public class OrderProcessorWorker : BackgroundService
{
    private readonly ILogger<OrderProcessorWorker> _logger;
    private readonly IConfiguration _configuration;
    private QueueClient? _queueClient;
    private string _connectionString;
    private string _queueName;

    public OrderProcessorWorker(ILogger<OrderProcessorWorker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _connectionString = _configuration["OrderProcessor:QueueConnectionString"] ?? "UseDevelopmentStorage=true";
        _queueName = _configuration["OrderProcessor:QueueName"] ?? "productcatalogorders";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _queueClient = new QueueClient(_connectionString, _queueName);
            await _queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);
            
            _logger.LogInformation("Order processing service started successfully. Watching queue: {QueueName}", _queueName);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var messages = await _queueClient.ReceiveMessagesAsync(
                        maxMessages: 1,
                        cancellationToken: stoppingToken);

                    if (messages.Value != null && messages.Value.Length > 0)
                    {
                        foreach (var message in messages.Value)
                        {
                            await ProcessMessageAsync(message, stoppingToken);
                            await _queueClient.DeleteMessageAsync(
                                message.MessageId,
                                message.PopReceipt,
                                cancellationToken: stoppingToken);
                        }
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error processing order");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting service");
            throw;
        }
        finally
        {
            _logger.LogInformation("Order processing service stopped.");
        }
    }

    private async Task ProcessMessageAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var messageText = message.Body.ToString();
            var order = JsonSerializer.Deserialize<Order>(messageText);

            if (order != null)
            {
                WriteOrderToConsole(order);
                _logger.LogInformation("Order {OrderId} processed successfully and removed from queue.", order.OrderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing or processing message");
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
}
