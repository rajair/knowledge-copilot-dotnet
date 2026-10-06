using KnowledgeCopilot.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddKnowledgeCopilotOptions();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<HeartbeatService>();

var host = builder.Build();

await host.RunAsync();
