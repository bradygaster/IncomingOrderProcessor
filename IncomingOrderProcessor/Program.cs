using IncomingOrderProcessor;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<OrderProcessorWorker>();

var host = builder.Build();
host.Run();
