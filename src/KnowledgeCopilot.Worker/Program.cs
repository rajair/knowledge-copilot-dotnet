using KnowledgeCopilot.Adapters.Azure;
using KnowledgeCopilot.Adapters.Fakes;
using KnowledgeCopilot.Adapters.Free;
using KnowledgeCopilot.Application;
using KnowledgeCopilot.ServiceDefaults;
using KnowledgeCopilot.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddKnowledgeCopilotOptions();

builder.Services.AddKnowledgeCopilotCore();

// The profile is read here, in the composition root, and nowhere else (D-02).
var options = builder.Configuration.GetSection(KnowledgeCopilotOptions.SectionName).Get<KnowledgeCopilotOptions>()
    ?? new KnowledgeCopilotOptions();
if (options.UseFakes)
{
    builder.AddFakeAdapters();
}
else
{
    _ = options.Profile switch
    {
        KnowledgeCopilotProfile.Free => builder.AddFreeAdapters(),
        KnowledgeCopilotProfile.Azure => builder.AddAzureAdapters(),
        _ => throw new InvalidOperationException("KnowledgeCopilot:Profile must be 'Free' or 'Azure'."),
    };
}

builder.Services.AddHostedService<HeartbeatService>();

var host = builder.Build();

await host.RunAsync();
