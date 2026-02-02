using IncomingOrderProcessor;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<OrderProcessorService>();

var host = builder.Build();
host.Run();
