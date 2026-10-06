# Decision log

Every significant choice, the alternatives considered and the trade-off
accepted. Each step's PR adds its own entries (`D-<step>-<nn>`) at the end of
this file. The foundational decisions below (`D-01` … `D-20`) were made while
planning the .NET port of the Python
[`rajair/knowledge-copilot`](https://github.com/rajair/knowledge-copilot) plan,
including fixes for gaps found during the plan review.

| ID   | Decision (short)                                                    |
| ---- | ------------------------------------------------------------------- |
| D-01 | .NET 10 LTS + Aspire 13 with a C# AppHost                           |
| D-02 | Hexagonal core; `Profile` chosen only at the composition root       |
| D-03 | Microsoft.Extensions.AI interfaces are the LLM and embedder ports   |
| D-04 | Own `IIndexStore` port, not `VectorData` as the port                |
| D-05 | Azure Storage Queues + Blob for both profiles (Azurite locally)     |
| D-06 | Add a metadata database (EF Core; PostgreSQL / Azure SQL)           |
| D-07 | C# records are the single source of truth for contracts             |
| D-08 | Embeddings, dimensions and sparse vectors                           |
| D-09 | Reranker via a TEI container in both profiles                       |
| D-10 | Parsing: built-in Markdown; docling-serve / Document Intelligence   |
| D-11 | AI SDK UI message stream v1, implemented in .NET                    |
| D-12 | Python RAGAS harness over HTTP; pinned judge; per-profile thresholds |
| D-13 | Step order: Chat API before the RAGAS gate                          |
| D-14 | ACL model: opaque principals, `acl_version`, separate job kinds     |
| D-15 | Server-verified action approvals                                    |
| D-16 | Backend-for-Frontend; browser never holds the access token          |
| D-17 | Bicep generated once, then owned and reviewed                       |
| D-18 | Makefile as the single entry point for humans and CI                |
| D-19 | Testing stack: xUnit v3, contract suites, Aspire.Hosting.Testing    |
| D-20 | Health endpoints exposed in every environment                       |

---

### D-01: .NET 10 LTS + Aspire 13 with a C# AppHost

- **Decision:** Target `net10.0` (LTS) pinned in `global.json`; use Aspire 13.x
  for local orchestration, service discovery, OpenTelemetry defaults and
  deployment manifests. The AppHost is written in C#.
- **Alternatives:** Docker Compose only (no dashboard, no service discovery,
  manual OTel); TypeScript AppHost (Aspire 13 supports it, but splits the
  orchestration language from the backend).
- **Trade-off:** Aspire versions move fast (13.x minor releases every few
  weeks). Mitigated by central package management and Dependabot.

### D-02: Hexagonal core; `Profile` chosen only at the composition root

- **Decision:** Same as the Python ADR-001. Domain and Application depend on
  ports. `Api/Program.cs` and `Worker/Program.cs` read
  `KnowledgeCopilot:Profile` once and call `AddFreeAdapters()` or
  `AddAzureAdapters()`. An architecture test fails the build if `Domain` or
  `Application` references a vendor SDK.
- **Alternatives:** `if (profile == Azure)` at call sites (rejected: leaks
  everywhere); two codebases (rejected: permission bugs would drift).

### D-03: Microsoft.Extensions.AI interfaces are the LLM and embedder ports

- **Decision:** Use `IChatClient` and `IEmbeddingGenerator<string, Embedding<float>>`
  directly as ports instead of writing `ILlm` / `IEmbedder`.
- **Why:** They are abstraction-only packages (no orchestration framework),
  have adapters for Ollama, OpenAI and Azure OpenAI, and include middleware
  for OpenTelemetry, caching and function invocation. This is not the
  "framework everything" option ADR-001 rejected: we take the interfaces, not
  an agent framework.
- **Trade-off:** We inherit their release cadence. We do **not** use automatic
  function invocation for action tools, because approvals need manual control
  (D-15).

### D-04: Own `IIndexStore` port, not `VectorData` as the port

- **Decision:** Define `IIndexStore` with explicit `SearchDense`,
  `SearchSparse`, `SearchHybrid`, ACL updates and an `IndexCapabilities`
  flag. Adapters may use `Microsoft.Extensions.VectorData` connectors
  internally.
- **Why:** ADR-001's point still holds: hybrid fusion, filter pushdown and
  security trimming differ between Qdrant and Azure AI Search, and they are
  exactly what we need to control and test. Separate dense and sparse calls
  also let us implement RRF by hand.

### D-05: Azure Storage Queues + Blob for both profiles (Azurite locally)

- **Decision:** Replace Celery + Redis and MinIO with one `IWorkQueue` adapter
  and one `IBlobStore` adapter on the Azure Storage SDKs. The free profile runs
  them against **Azurite** (MIT-licensed emulator, first-class in Aspire).
- **Why:** The visibility timeout gives the same at-least-once semantics as
  Celery's `acks_late`; one adapter instead of two; KEDA scales the worker on
  queue length in ACA.
- **Alternatives:** Redis Streams (extra adapter); MassTransit (v9 needs a
  commercial licence); Wolverine (more framework than we need).
- **Trade-off:** The free profile uses an emulator of a cloud service rather
  than a "native" OSS broker. The port still allows a Redis Streams adapter
  later.

### D-06: Add a metadata database (EF Core; PostgreSQL / Azure SQL)

- **Decision:** Add a system-of-record database for documents, ACLs, the chunk
  ledger, ingestion jobs, conversations, approvals and usage. EF Core with
  **PostgreSQL** in `free` and **Azure SQL Database (free offer)** in `azure`;
  one migrations project per provider.
- **Why:** The Python plan had no database. Without one there is nowhere to
  keep document status, the hash ledger, approvals or audit records, and the
  index can't be rebuilt.
- **Alternatives:** PostgreSQL in both (Azure Flexible Server B1ms costs about
  half the monthly budget); Cosmos DB free tier (weaker relational
  guarantees for approvals and jobs); index-only (no transactions).
- **Trade-off:** Two migration sets. CI applies both to catch drift.

### D-07: C# records are the single source of truth for contracts

- **Decision:** Contracts live in `KnowledgeCopilot.Contracts` as C# records
  with `System.Text.Json` attributes (including `[JsonPolymorphic]` for widget
  variants). The build exports **OpenAPI 3.1** (built-in
  `Microsoft.AspNetCore.OpenApi`) and **JSON Schema** (`JsonSchemaExporter`)
  into `contracts/schema/`. Codegen produces TypeScript types + zod schemas for
  `web/` and Pydantic models for `evals/`. CI regenerates and fails on
  `git diff`.
- **Why:** Three languages consume the same payloads. Tool schemas sent to the
  LLM come from the same records (via `AIFunctionFactory`/`AIJsonUtilities`).

### D-08: Embeddings, dimensions and sparse vectors

- **Decision:** `free`: an Ollama embedding model (configurable, default
  `nomic-embed-text`). `azure`: `text-embedding-3-small` at **512 dimensions**
  so the AI Search Free tier (50 MB) holds a useful corpus. Sparse retrieval:
  Azure AI Search uses its built-in BM25; Qdrant uses client-computed term
  frequencies in a sparse vector with Qdrant's `idf` modifier (BM25-style).
- **Trade-off:** Different embedders mean absolute eval scores differ per
  profile (see D-12). The embedding model and dimensions are recorded in the
  ledger, so changing them forces a re-embed.

### D-09: Reranker via a TEI container in both profiles

- **Decision:** `IReranker` is implemented by `TeiReranker` calling Hugging
  Face text-embeddings-inference with a `bge-reranker` model (CPU).
  `PassThroughReranker` is the fallback when it's disabled.
- **Alternatives:** In-process ONNX Runtime + tokenizer (no container, more
  code); Azure AI Search semantic ranker (not available on Free).
- **Trade-off:** Cold start on ACA when scaled to zero; the retriever enforces
  a timeout and falls back to RRF order.

### D-10: Parsing: built-in Markdown; docling-serve / Document Intelligence

- **Decision:** The main corpus (GitLab Handbook) is Markdown, parsed in-process
  with Markdig into heading-aware sections. PDFs (10-Ks) go through
  `docling-serve` (free) or Document Intelligence F0 (azure).

### D-11: AI SDK UI message stream v1, implemented in .NET

- **Decision:** `POST /api/v1/chat` emits Server-Sent Events in the AI SDK UI
  message stream protocol (`x-vercel-ai-ui-message-stream: v1`): `start`,
  `start-step`, `text-start` / `text-delta` / `text-end`,
  `tool-input-available`, `tool-approval-request`, `tool-output-available`,
  `tool-output-denied`, `source-document`, `data-*`, `error`, `finish-step`,
  `finish`, then `data: [DONE]`. Golden tests snapshot the exact byte stream.
- **Why:** `useChat` works unchanged; the protocol is documented; no custom
  client parser.
- **Trade-off:** The protocol evolves with AI SDK majors (currently `ai` 7.x /
  `@ai-sdk/react` 4.x). Versions are pinned and the golden tests catch drift.

### D-12: Python RAGAS harness over HTTP; pinned judge; per-profile thresholds

- **Decision:** `evals/` is a uv project (RAGAS 0.4.x) that only talks to the
  system through `POST /api/v1/answers` and `POST /api/v1/search`. The judge
  LLM is **pinned and identical for both profiles** (GitHub Models in CI via
  `permissions: models: read`, or Azure OpenAI). Thresholds are stored per
  profile; the **ACL-leak count must be 0** in both.
- **Why:** The two profiles use different generators and embedders, so
  identical scores are not expected. "Swapping clouds is a config change" is
  proven by both passing their gates on the same golden set and the same
  judge.

### D-13: Step order: Chat API before the RAGAS gate

- **Decision:** Retrieval metrics (recall@k, MRR, ACL leaks) land in Step 4.
  The Chat/Answers API is Step 5. The RAGAS gate (faithfulness, answer
  relevancy) is Step 6.
- **Why:** In the Python plan, evals (Step 5) came before the chat API
  (Step 6), but answer metrics need generated answers.

### D-14: ACL model: opaque principals, `acl_version`, separate job kinds

- **Decision:** Principals are opaque strings `u:{userId}`, `g:{groupId}`,
  `t:{tenantId}`. Keycloak group IDs and Entra group object IDs both fit.
  Every document has an `acl_version`. ACL changes and deletions are their own
  job kinds (`UpdateAcl`, `Delete`) and never go through content hashing.
- **Why:** In the Python plan an ACL-only change kept the same content hash, so
  it would have been skipped and left stale permissions in the index.

### D-15: Server-verified action approvals

- **Decision:** Action tools (`proposeAction`) never auto-execute. The api
  stores a `PENDING_APPROVAL` row (user, tool call id, args hash, expiry), emits
  `tool-approval-request`, and only executes when a later request carries an
  approval that matches all of these. Approvals are single use.
- **Why:** Approval that only lives in the browser can be forged or replayed.

### D-16: Backend-for-Frontend; browser never holds the access token

- **Decision:** Next.js route handlers own the OIDC session (encrypted httpOnly
  cookie) and forward the access token to the api. The specific library
  (Auth.js or an alternative) is chosen in Step 8 after checking its
  maintenance status and Next.js 16 support.

### D-17: Bicep generated once, then owned and reviewed

- **Decision:** Generate the initial Bicep from the AppHost (`aspire publish` /
  `azd infra gen`), commit it under `infra/`, then edit it by hand: budget alert,
  Free-tier SKUs, managed identity role assignments, KEDA scaler, no keys.
  GitHub Actions runs `what-if` on PRs and deploys on `main` using OIDC.
- **Why:** Generated Bicep is a fast, correct start; owning it is where the
  learning (and the cost control) happens.

### D-18: Makefile as the single entry point for humans and CI

- **Decision:** `make setup | build | lint | test | dev | contracts | eval`
  wraps `dotnet`, `pnpm` and `uv`. CI calls the same targets.
- **Why:** Same as the Python project: "passes locally" means "passes in CI".
  On Windows, GNU make comes from `winget install ezwinports.make`.

### D-19: Testing stack: xUnit v3, contract suites, Aspire.Hosting.Testing

- **Decision:** xUnit v3 for .NET tests; one abstract contract-test class per
  port, subclassed per adapter (fakes always; real adapters against Aspire- or
  container-hosted backends); `Aspire.Hosting.Testing` for end-to-end tests;
  Vitest + Testing Library for `web/`; pytest for `evals/`.

### D-20: Health endpoints exposed in every environment

- **Decision:** The Aspire ServiceDefaults template maps `/health` and `/alive`
  only in Development. We map them in all environments (anonymous, cheap, no
  details beyond status) because Container Apps and Compose need them for
  probes.

---

## Step decisions

Each step appends its decisions below as `D-<step>-<nn>` with the same
structure (decision, alternatives, trade-off).
