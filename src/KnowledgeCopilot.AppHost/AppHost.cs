var builder = DistributedApplication.CreateBuilder(args);

// The AppHost is a composition root: it is one of the few places allowed to choose the profile.
const string Profile = "Free";

var api = builder.AddProject<Projects.KnowledgeCopilot_Api>("api")
    .WithEnvironment("KnowledgeCopilot__Profile", Profile)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.KnowledgeCopilot_Worker>("worker")
    .WithEnvironment("KnowledgeCopilot__Profile", Profile)
    .WaitFor(api);

// AddNextJsApp is marked experimental in Aspire 13.6 (D-0-03).
#pragma warning disable ASPIREJAVASCRIPT001
builder.AddNextJsApp("web", "../../web")
#pragma warning restore ASPIREJAVASCRIPT001
    .WithPnpm(install: true, installArgs: ["--frozen-lockfile"])
    .WithReference(api)
    .WaitFor(api)
    .WithHttpHealthCheck("/api/health");

await builder.Build().RunAsync();
