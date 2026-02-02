using IncomingOrderProcessor;

var builder = Host.CreateApplicationBuilder(args);

// Add configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add the worker service
builder.Services.AddHostedService<OrderProcessorWorker>();

var host = builder.Build();
host.Run();
