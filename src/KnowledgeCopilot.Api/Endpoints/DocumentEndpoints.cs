using KnowledgeCopilot.Api.OpenApi;
using KnowledgeCopilot.Contracts.Documents;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeCopilot.Api.Endpoints;

/// <summary>Document ingestion and ACL routes (architecture section 10). Stubs until Step 3.</summary>
internal static class DocumentEndpoints
{
    private const int ImplementedIn = 3;

    public static RouteGroupBuilder MapDocumentEndpoints(this RouteGroupBuilder api)
    {
        var documents = api.MapGroup("/documents").WithTags("Documents");

        // Multipart: a `file` part and a `metadata` part holding DocumentUploadMetadata as JSON (D-1-05).
        // Antiforgery is off because the Api only accepts bearer tokens from the BFF, never cookies.
        documents.MapPost("/", Upload)
            .WithName("UploadDocument")
            .WithMetadata(new JsonFormPartMetadata(UploadMetadataPart, typeof(DocumentUploadMetadata)))
            .DisableAntiforgery();
        documents.MapGet("/", List).WithName("ListDocuments");
        documents.MapGet("/{id:guid}", Get).WithName("GetDocument");
        documents.MapPut("/{id:guid}/acl", ReplaceAcl).WithName("ReplaceDocumentAcl");
        documents.MapDelete("/{id:guid}", Delete).WithName("DeleteDocument");

        return api;
    }

    internal const string UploadMetadataPart = "metadata";

    private static Results<Accepted<DocumentDto>, ProblemHttpResult> Upload(
        IFormFile file,
        [FromForm(Name = UploadMetadataPart)] string metadata) =>
        NotImplementedYet.Problem(ImplementedIn);

    private static Results<Ok<IReadOnlyList<DocumentDto>>, ProblemHttpResult> List() =>
        NotImplementedYet.Problem(ImplementedIn);

    private static Results<Ok<DocumentDto>, ProblemHttpResult> Get(Guid id) =>
        NotImplementedYet.Problem(ImplementedIn);

    private static Results<Accepted, ProblemHttpResult> ReplaceAcl(Guid id, AclRequest acl) =>
        NotImplementedYet.Problem(ImplementedIn);

    private static Results<Accepted, ProblemHttpResult> Delete(Guid id) =>
        NotImplementedYet.Problem(ImplementedIn);
}
