using IncomingOrderProcessor.WorkerService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

// Add Windows Services support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "IncomingOrderProcessor";
});

var host = builder.Build();
host.Run();
