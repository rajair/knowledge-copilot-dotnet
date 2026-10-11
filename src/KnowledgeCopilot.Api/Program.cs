var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddKnowledgeCopilotOptions();

var app = builder.Build();

app.MapDefaultEndpoints();

await app.RunAsync();
