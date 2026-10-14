using KnowledgeCopilot.Adapters.Azure;
using KnowledgeCopilot.Adapters.Fakes;
using KnowledgeCopilot.Adapters.Free;
using KnowledgeCopilot.Api.Endpoints;
using KnowledgeCopilot.Api.OpenApi;
using KnowledgeCopilot.Application;
using KnowledgeCopilot.Contracts.Serialization;
using KnowledgeCopilot.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

if (BuildTimeDocument.IsGenerating)
{
    BuildTimeDocument.UseFixedProfile(builder.Configuration);
}

builder.AddServiceDefaults();
builder.AddKnowledgeCopilotOptions();

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options => ContractsJson.Apply(options.SerializerOptions));
builder.Services.AddContractsOpenApi();

builder.Services.AddKnowledgeCopilotCore();

// Build-time OpenAPI generation only needs the endpoint metadata, so it skips the adapters.
if (!BuildTimeDocument.IsGenerating)
{
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

        if (builder.Environment.IsDevelopment())
        {
            builder.AddDevCurrentPrincipal();
        }
    }
}

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var api = app.MapGroup("/api/v1")
    .ProducesProblem(StatusCodes.Status501NotImplemented);
api.MapDocumentEndpoints();
api.MapSearchEndpoints();
api.MapAnswerEndpoints();
api.MapChatEndpoints();

await app.RunAsync();
