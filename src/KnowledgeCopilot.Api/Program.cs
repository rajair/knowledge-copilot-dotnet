using KnowledgeCopilot.Api.Endpoints;
using KnowledgeCopilot.Api.OpenApi;
using KnowledgeCopilot.Contracts.Serialization;

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
