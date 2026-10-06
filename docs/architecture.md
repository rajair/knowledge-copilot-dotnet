# Architecture

Target architecture for **Knowledge Copilot (.NET)**: a permission-aware RAG
copilot with generative UI, built on .NET 10 and Aspire 13, with a Python
evaluation harness. One codebase, two deployment profiles (`free` and `azure`).

Every step page in [`docs/steps`](steps/README.md) builds a slice of this
picture. Design choices and their alternatives are in
[decisions.md](decisions.md).

- [1. Goals and quality attributes](#1-goals-and-quality-attributes)
- [2. System context](#2-system-context)
- [3. Containers per profile](#3-containers-per-profile)
- [4. Solution structure](#4-solution-structure)
- [5. Components inside api and worker](#5-components-inside-api-and-worker)
- [6. Ports and adapters](#6-ports-and-adapters)
- [7. Data model](#7-data-model)
- [8. Search index schema and ACL model](#8-search-index-schema-and-acl-model)
- [9. Sequence diagrams](#9-sequence-diagrams)
- [10. API surface](#10-api-surface)
- [11. Security model](#11-security-model)
- [12. Observability](#12-observability)
- [13. Configuration](#13-configuration)
- [14. Deployment](#14-deployment)
- [15. Initial SLOs and budgets](#15-initial-slos-and-budgets)

---

## 1. Goals and quality attributes

| Goal                                                  | How the architecture achieves it                                                                                         |
| ----------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| Users only ever see content they are allowed to see   | ACL filter is pushed **into** every index query; tenant is mandatory; caches are keyed by principal set                  |
| Answers are grounded and cite their sources           | Retrieved chunks are the only context; citations are validated against the retrieved set; refuse when context is empty |
| Rich answers (charts, tables, actions), not just text | Typed tool calls streamed in the AI SDK UI message stream and rendered as React components                               |
| Side effects never happen without a human             | Action tools require an approval round-trip that the server verifies                                                     |
| Swap cloud for local OSS with a config change         | Hexagonal core; `Profile` selects adapters at the composition root only                                                  |
| Quality is measured, not guessed                      | Python RAGAS harness runs the same golden set against both profiles; CI gate                                             |
| Cheap to run                                          | `free` costs $0; `azure` targets ~$10–25/month using free tiers and scale-to-zero                                        |
| Observable end to end                                 | OpenTelemetry from day one via Aspire ServiceDefaults; GenAI spans, TTFT and cost metrics                                |

## 2. System context

```mermaid
flowchart LR
  employee(["Employee<br/>asks questions"])
  admin(["Knowledge admin<br/>uploads documents, sets ACLs"])
  dev(["Developer / CI"])

  subgraph kc["Knowledge Copilot"]
    system["Web app + API + Worker"]
  end

  idp["Identity provider<br/>Keycloak or Entra ID"]
  llm["LLM + embeddings<br/>Ollama / GitHub Models or Azure OpenAI"]
  sources["Public corpora<br/>GitLab Handbook, SEC 10-Ks"]
  evals["Eval harness<br/>Python + RAGAS"]

  employee -->|"chat, view citations, approve actions"| system
  admin -->|"upload, edit ACL, delete"| system
  system -->|"OIDC login, JWKS"| idp
  system -->|"chat completions, embeddings"| llm
  dev -->|"download scripts seed corpus"| sources
  dev -->|"run golden set"| evals
  evals -->|"POST /api/v1/answers"| system
```

## 3. Containers per profile

Both profiles run the **same three app images** (`web`, `api`, `worker`). Only
the backing services behind each port differ.

### 3.1 `Profile=Free` (local, OSS, $0), orchestrated by the Aspire AppHost

```mermaid
flowchart LR
  user(["Browser"])

  subgraph apphost["Aspire AppHost (dev) / Docker Compose (Step 10)"]
    subgraph web["web: Next.js 16"]
      ui["React UI<br/>useChat + widget components"]
      bff["Route handlers (BFF)<br/>OIDC session, token forwarding"]
    end
    subgraph api["api: ASP.NET Core 10"]
      chat["Chat orchestrator"]
      retr["Hybrid retriever"]
      docs["Document endpoints"]
    end
    subgraph worker["worker: .NET Worker Service"]
      ingest["Ingestion pipeline"]
    end

    pg[("PostgreSQL<br/>metadata, ledger, approvals")]
    azurite[("Azurite<br/>Blob + Queue emulator")]
    qdrant[("Qdrant<br/>dense + sparse")]
    ollama["Ollama<br/>chat + embedding models"]
    tei["TEI reranker<br/>bge-reranker"]
    docling["docling-serve<br/>PDF parsing"]
    keycloak["Keycloak<br/>OIDC + groups"]
    dash["Aspire dashboard<br/>OTLP traces, logs, metrics"]
    phoenix["Arize Phoenix<br/>LLM traces (optional)"]
  end

  user --> ui
  ui --> bff
  user -->|login| keycloak
  bff -->|"Bearer JWT + SSE"| chat
  bff --> docs
  chat --> retr
  chat --> ollama
  chat --> pg
  retr --> qdrant
  retr --> ollama
  retr --> tei
  docs --> azurite
  docs --> pg
  azurite -->|queue message| ingest
  ingest --> azurite
  ingest --> docling
  ingest --> ollama
  ingest --> qdrant
  ingest --> pg
  api -.->|verify JWT via JWKS| keycloak
  api -.->|OTLP| dash
  worker -.->|OTLP| dash
  api -.->|OTLP| phoenix
```

### 3.2 `Profile=Azure` (managed, scale-to-zero)

```mermaid
flowchart LR
  user(["Browser"])
  gh["GitHub Actions<br/>OIDC federated credential"]

  subgraph rg["Resource group"]
    subgraph aca["Container Apps environment"]
      web["web<br/>Next.js 16"]
      api["api<br/>ASP.NET Core 10"]
      worker["worker<br/>KEDA queue scaler"]
      tei["reranker<br/>TEI container"]
    end
    mi["User-assigned managed identity"]
    acr["Container Registry (Basic)"]
    aoai["Azure OpenAI<br/>chat + embeddings"]
    search[("Azure AI Search<br/>Free tier")]
    st[("Storage account<br/>Blob + Queue")]
    sql[("Azure SQL Database<br/>free offer")]
    di["Document Intelligence F0"]
    kv["Key Vault"]
    appi["Application Insights<br/>+ Log Analytics"]
  end
  entra["Entra ID"]

  user --> web
  user -->|login| entra
  web -->|"Bearer JWT + SSE"| api
  api -.->|JWKS| entra
  api --> aoai
  api --> search
  api --> tei
  api --> st
  api --> sql
  st -->|queue length scales| worker
  worker --> st
  worker --> di
  worker --> aoai
  worker --> search
  worker --> sql
  aca -.->|"RBAC data-plane roles"| mi
  mi -.-> kv
  aca -.->|"Azure Monitor OTel distro"| appi
  acr --> aca
  gh -->|"azd deploy / Bicep"| rg
```

## 4. Solution structure

```text
knowledge-copilot-dotnet/
├── global.json                      # pins .NET 10 SDK
├── KnowledgeCopilot.slnx            # solution (XML .slnx format)
├── Directory.Build.props            # net10.0, nullable, warnings-as-errors, analyzers
├── Directory.Packages.props         # central package versions
├── Makefile                         # setup · build · lint · test · dev · contracts · eval
├── src/
│   ├── KnowledgeCopilot.AppHost/            # Aspire orchestration for both profiles
│   ├── KnowledgeCopilot.ServiceDefaults/    # OTel, health, resilience, service discovery
│   ├── KnowledgeCopilot.Contracts/          # C# records: the single source of truth for payloads
│   ├── KnowledgeCopilot.Domain/             # ports (interfaces) + pure domain logic, no vendor SDKs
│   ├── KnowledgeCopilot.Application/        # use cases: ingestion, retrieval, chat, approvals
│   ├── KnowledgeCopilot.Persistence/        # EF Core DbContext + entities
│   ├── KnowledgeCopilot.Persistence.Migrations.Postgres/
│   ├── KnowledgeCopilot.Persistence.Migrations.SqlServer/
│   ├── KnowledgeCopilot.Adapters.Common/    # Storage Blob/Queue, TEI reranker, Markdown parser
│   ├── KnowledgeCopilot.Adapters.Free/      # Qdrant, Ollama, docling-serve
│   ├── KnowledgeCopilot.Adapters.Azure/     # AI Search, Azure OpenAI, Doc Intelligence, Graph overage
│   ├── KnowledgeCopilot.Adapters.Fakes/     # in-memory test doubles for every port
│   ├── KnowledgeCopilot.Api/                # minimal API host + composition root
│   └── KnowledgeCopilot.Worker/             # queue consumer host + composition root
├── tests/
│   ├── KnowledgeCopilot.Domain.Tests/
│   ├── KnowledgeCopilot.Application.Tests/
│   ├── KnowledgeCopilot.Adapters.ContractTests/   # one abstract suite per port, run per adapter
│   ├── KnowledgeCopilot.Architecture.Tests/       # dependency rules
│   ├── KnowledgeCopilot.Api.Tests/
│   └── KnowledgeCopilot.AppHost.Tests/      # Aspire.Hosting.Testing end-to-end
├── web/                                     # Next.js 16 app (pnpm)
│   └── src/contracts/                       # generated TS types + zod schemas
├── evals/                                   # Python 3.12 + uv + RAGAS
│   └── src/knowledge_evals/contracts/       # generated Pydantic models
├── contracts/schema/                        # generated, committed OpenAPI + JSON Schema
├── tools/                                   # codegen + dataset download scripts
├── infra/                                   # Bicep (Step 11)
├── deploy/                                  # Compose output, Keycloak realm, seed data (Step 10)
├── loadtests/                               # k6 scenarios (Step 12)
└── docs/
```

**Dependency rule** (enforced by an architecture test in Step 2):

```mermaid
flowchart BT
  contracts["Contracts"]
  domain["Domain<br/>ports + pure logic"]
  app["Application<br/>use cases"]
  persist["Persistence"]
  adapters["Adapters.*"]
  hosts["Api / Worker<br/>composition roots"]

  domain --> contracts
  app --> domain
  persist --> domain
  adapters --> domain
  hosts --> app
  hosts --> adapters
  hosts --> persist
```

`Domain` and `Application` never reference a vendor SDK. Only the hosts know
which adapter implements which port.

## 5. Components inside api and worker

```mermaid
flowchart TB
  subgraph api["KnowledgeCopilot.Api"]
    mw["Middleware<br/>auth · rate limit · ProblemDetails · OTel"]
    ep_docs["Documents endpoints"]
    ep_chat["Chat endpoint (SSE)"]
    ep_ans["Answers endpoint (JSON, evals)"]
    enc["UiMessageStreamWriter<br/>AI SDK v1 protocol"]
  end

  subgraph appl["KnowledgeCopilot.Application"]
    ingest_uc["IngestionService"]
    retr_uc["HybridRetriever"]
    chat_uc["ChatOrchestrator"]
    tools["ToolRegistry<br/>showFinancialChart · showComparisonTable · proposeAction"]
    approvals["ApprovalService + ActionExecutor"]
    guard["Guardrails<br/>input checks · context fencing · citation validation"]
    cost["UsageRecorder"]
  end

  subgraph dom["KnowledgeCopilot.Domain"]
    chunker["StructureAwareChunker"]
    hasher["ContentHasher (SHA-256)"]
    rrf["ReciprocalRankFusion"]
    acl["AclFilter builder"]
    prompt["PromptBuilder"]
    ports["Ports (interfaces)"]
  end

  subgraph wrk["KnowledgeCopilot.Worker"]
    consumer["QueueConsumer<br/>visibility timeout · poison handling"]
    handlers["Job handlers<br/>Index · UpdateAcl · Delete"]
  end

  mw --> ep_docs
  mw --> ep_chat
  mw --> ep_ans
  ep_docs --> ingest_uc
  ep_chat --> chat_uc
  ep_ans --> chat_uc
  chat_uc --> retr_uc
  chat_uc --> tools
  chat_uc --> guard
  chat_uc --> prompt
  chat_uc --> cost
  chat_uc --> enc
  tools --> approvals
  retr_uc --> rrf
  retr_uc --> acl
  consumer --> handlers
  handlers --> ingest_uc
  ingest_uc --> chunker
  ingest_uc --> hasher
  appl --> ports
```

## 6. Ports and adapters

```mermaid
classDiagram
  direction LR
  class IChatClient {
    <<Microsoft.Extensions.AI>>
    +GetResponseAsync()
    +GetStreamingResponseAsync()
  }
  class IEmbeddingGenerator {
    <<Microsoft.Extensions.AI>>
    +GenerateAsync(texts)
  }
  class IIndexStore {
    <<port>>
    +Capabilities IndexCapabilities
    +EnsureIndexAsync()
    +UpsertAsync(chunks)
    +DeleteByDocumentAsync(tenant, documentId)
    +DeleteChunksAsync(tenant, chunkIds)
    +UpdateAclAsync(tenant, documentId, principals, aclVersion)
    +SearchDenseAsync(vector, aclFilter, k)
    +SearchSparseAsync(text, aclFilter, k)
    +SearchHybridAsync(text, vector, aclFilter, k)
  }
  class IReranker {
    <<port>>
    +RerankAsync(query, candidates, topN)
  }
  class IBlobStore {
    <<port>>
    +PutAsync(key, stream, metadata)
    +OpenReadAsync(key)
    +DeleteAsync(key)
  }
  class IWorkQueue {
    <<port>>
    +EnqueueAsync(jobMessage)
    +ReceiveAsync(maxMessages, visibility)
    +CompleteAsync(receipt)
    +DeadLetterAsync(receipt, reason)
  }
  class IDocumentParser {
    <<port>>
    +CanParse(contentType)
    +ParseAsync(stream, contentType)
  }
  class ICurrentPrincipal {
    <<port>>
    +TenantId
    +UserId
    +Groups
    +ToAclPrincipals()
  }

  IIndexStore <|.. QdrantIndexStore : free
  IIndexStore <|.. AzureSearchIndexStore : azure
  IIndexStore <|.. InMemoryIndexStore : tests
  IReranker <|.. TeiReranker : both
  IReranker <|.. PassThroughReranker : fallback
  IBlobStore <|.. AzureBlobStore : Azurite or Azure
  IBlobStore <|.. InMemoryBlobStore : tests
  IWorkQueue <|.. StorageQueueWorkQueue : Azurite or Azure
  IWorkQueue <|.. InMemoryWorkQueue : tests
  IDocumentParser <|.. MarkdownParser : both
  IDocumentParser <|.. DoclingParser : free
  IDocumentParser <|.. DocumentIntelligenceParser : azure
  ICurrentPrincipal <|.. JwtCurrentPrincipal : both
  ICurrentPrincipal <|.. DevCurrentPrincipal : Development only
```

| Port                  | `Free` adapter                                   | `Azure` adapter                        | Test double              |
| --------------------- | ------------------------------------------------ | -------------------------------------- | ------------------------ |
| `IChatClient`         | OllamaSharp (or GitHub Models via OpenAI client) | Azure OpenAI via `Azure.AI.OpenAI`     | `ScriptedChatClient`     |
| `IEmbeddingGenerator` | OllamaSharp embedding model                      | Azure OpenAI `text-embedding-3-small`  | `HashEmbeddingGenerator` |
| `IIndexStore`         | Qdrant (dense + sparse BM25 with IDF modifier)   | Azure AI Search (vector + BM25, RRF)   | `InMemoryIndexStore`     |
| `IReranker`           | TEI container                                    | TEI container on ACA                   | `PassThroughReranker`    |
| `IBlobStore`          | Azure Blob SDK → Azurite                         | Azure Blob SDK → Storage account       | `InMemoryBlobStore`      |
| `IWorkQueue`          | Storage Queue SDK → Azurite                      | Storage Queue SDK → Storage account    | `InMemoryWorkQueue`      |
| `IDocumentParser`     | Markdown (built-in) + docling-serve              | Markdown (built-in) + Doc Intelligence | Markdown only            |
| Metadata DB           | EF Core + Npgsql → PostgreSQL                    | EF Core + SqlServer → Azure SQL        | EF Core SQLite in-memory |
| Identity              | JwtBearer → Keycloak                             | JwtBearer → Entra ID (+ Graph overage) | `DevCurrentPrincipal`    |
| Telemetry exporter    | OTLP → Aspire dashboard / Phoenix                | Azure Monitor OTel distro              | in-memory exporter       |

`IndexCapabilities` lets the retriever choose: if `NativeHybrid` is true and
the request does not ask for manual fusion, call `SearchHybridAsync`; otherwise
run dense and sparse in parallel and fuse with the domain `ReciprocalRankFusion`.

## 7. Data model

The metadata database is the system of record for documents, ACLs, ingestion
state, approvals and usage. The search index is a **derived** projection and
can be rebuilt from the database plus blob storage.

```mermaid
erDiagram
  DOCUMENT ||--o{ DOCUMENT_ACL : "grants"
  DOCUMENT ||--o{ CHUNK_LEDGER : "indexed as"
  DOCUMENT ||--o{ INGESTION_JOB : "processed by"
  CONVERSATION ||--o{ MESSAGE : "contains"
  CONVERSATION ||--o{ PENDING_APPROVAL : "raises"
  PENDING_APPROVAL ||--o| ACTION_RECORD : "executes"
  CONVERSATION ||--o{ USAGE_RECORD : "costs"

  DOCUMENT {
    uuid id PK
    string tenant_id
    string title
    string source_uri
    string content_type
    string blob_key
    string content_sha256
    int version
    int acl_version
    string status "Pending, Processing, Indexed, Failed, Deleting, Deleted"
    string last_error
    string created_by
    datetime created_at
    datetime updated_at
  }
  DOCUMENT_ACL {
    uuid document_id FK
    string principal "u:userId or g:groupId or t:tenantId"
  }
  CHUNK_LEDGER {
    string chunk_id PK "sha256 of tenant, doc, content hash, occurrence"
    uuid document_id FK
    int ordinal
    string content_sha256
    string embedding_model
    datetime indexed_at
  }
  INGESTION_JOB {
    uuid id PK
    uuid document_id FK
    string kind "Index, UpdateAcl, Delete"
    string idempotency_key UK
    string status "Queued, Running, Succeeded, Failed, DeadLettered"
    int attempts
    string last_error
    datetime created_at
    datetime finished_at
  }
  CONVERSATION {
    uuid id PK
    string tenant_id
    string user_id
    datetime created_at
  }
  MESSAGE {
    uuid id PK
    uuid conversation_id FK
    string role
    json parts
    datetime created_at
  }
  PENDING_APPROVAL {
    string approval_id PK
    string tool_call_id
    uuid conversation_id FK
    string user_id
    string tool_name
    string args_sha256
    json args
    string status "Pending, Approved, Denied, Expired, Executed"
    datetime expires_at
  }
  ACTION_RECORD {
    uuid id PK
    string approval_id FK
    string tool_name
    json result
    datetime executed_at
  }
  USAGE_RECORD {
    uuid id PK
    uuid conversation_id FK
    string tenant_id
    string user_id
    string model
    int input_tokens
    int output_tokens
    decimal cost_usd
    int ttft_ms
    int retrieval_ms
    datetime created_at
  }
```

## 8. Search index schema and ACL model

One index (`kc-chunks`) per environment. Every chunk carries its tenant and ACL
principals, so the filter runs **inside** the search engine (pre-filtering).
Filtering after retrieval would both leak (scores, counts, timing) and starve
top-k.

| Field                           | Type                        | Purpose                                                      |
| ------------------------------- | --------------------------- | ------------------------------------------------------------ |
| `chunk_id`                      | key                         | Deterministic; makes upserts idempotent                      |
| `tenant_id`                     | keyword, filterable         | Mandatory filter; Qdrant payload index with `is_tenant=true` |
| `document_id`                   | keyword, filterable         | Delete and ACL update by document                            |
| `acl_principals`                | keyword[], filterable       | `u:…`, `g:…`, `t:…` (whole tenant)                           |
| `acl_version`                   | int                         | Detects stale ACL projections                                |
| `title`, `heading_path`, `page` | text / keyword              | Citations and display                                        |
| `text`                          | text (BM25)                 | Sparse/keyword retrieval and LLM context                     |
| `content_sha256`                | keyword                     | Change detection                                             |
| `dense`                         | vector (dims from embedder) | Semantic retrieval                                           |
| `sparse`                        | sparse vector (Qdrant only) | BM25-style term weights; Qdrant applies the IDF modifier     |

**Filter, built once in `Domain.AclFilter` and translated by each adapter:**

```text
tenant_id == principal.TenantId
AND acl_principals ANY OF [ "u:{userId}", "g:{group1}", …, "t:{tenantId}" ]
```

- Qdrant: `must: [ match tenant_id, match_any acl_principals ]`
- Azure AI Search: `tenant_id eq 'T' and acl_principals/any(p: search.in(p, 'u:x,g:y,t:T', ','))`

**Capacity note.** AI Search Free allows 50 MB of storage. At 1536 dimensions ×
4 bytes a vector is ~6 KB, so a few thousand chunks fill it. The `azure`
profile therefore requests 512-dimension embeddings
(`text-embedding-3-small` supports reduced dimensions) and the seed corpus is
capped. See [D-08](decisions.md#d-08-embeddings-dimensions-and-sparse-vectors).

## 9. Sequence diagrams

### 9.1 Local developer startup (Aspire)

```mermaid
sequenceDiagram
  autonumber
  actor Dev as Developer
  participant CLI as aspire run
  participant AH as AppHost
  participant C as Containers
  participant API as api
  participant WK as worker
  participant WEB as web
  participant DB as Aspire dashboard

  Dev->>CLI: aspire run (profile=free)
  CLI->>AH: build and start
  AH->>C: start postgres, azurite, qdrant, ollama, tei, docling, keycloak
  AH->>C: wait for health checks
  AH->>API: start with connection strings + KnowledgeCopilot__Profile=Free
  API->>API: ValidateOnStart options, apply EF migrations (dev only)
  AH->>WK: start (WaitFor api, azurite, qdrant)
  AH->>WEB: pnpm dev with API URL injected via service discovery
  API-->>DB: OTLP logs, traces, metrics
  WK-->>DB: OTLP logs, traces, metrics
  Dev->>DB: open dashboard, see all resources healthy
```

### 9.2 Document upload

```mermaid
sequenceDiagram
  autonumber
  actor Admin as Knowledge admin
  participant WEB as web BFF
  participant API as api
  participant DB as Metadata DB
  participant BL as IBlobStore
  participant Q as IWorkQueue

  Admin->>WEB: upload file + ACL (groups, users)
  WEB->>API: POST /api/v1/documents (multipart, Bearer JWT)
  API->>API: authorize (role kc.admin), validate size, type, ACL principals
  API->>API: compute SHA-256 while streaming to a temp buffer
  API->>DB: find document by tenant + source_uri
  alt same content hash and same ACL
    API-->>WEB: 200 OK (unchanged, no job)
  else new or changed
    API->>BL: PutAsync(tenant/docId/version, stream)
    API->>DB: upsert DOCUMENT (status Pending), replace DOCUMENT_ACL
    API->>DB: insert INGESTION_JOB (kind Index, idempotency key docId:version)
    API->>Q: EnqueueAsync(JobMessage with jobId, kind, traceparent)
    API-->>WEB: 202 Accepted + Location /api/v1/documents/{id}
  end
  Note over API,Q: Only ids go on the queue, never content or ACLs.<br/>If enqueue fails after commit, a sweeper re-enqueues Queued jobs older than N minutes.
```

### 9.3 Worker indexing (idempotent, at-least-once)

```mermaid
sequenceDiagram
  autonumber
  participant Q as IWorkQueue
  participant WK as worker QueueConsumer
  participant DB as Metadata DB
  participant BL as IBlobStore
  participant P as IDocumentParser
  participant CH as Chunker + Hasher
  participant EM as IEmbeddingGenerator
  participant IX as IIndexStore

  Q-->>WK: message (visibility timeout 5 min)
  WK->>WK: restore trace context from traceparent
  WK->>DB: load job, if Succeeded then complete message and stop
  WK->>DB: job Running, attempts + 1, document Processing
  WK->>BL: OpenReadAsync(blob_key)
  WK->>P: ParseAsync(stream, content_type)
  P-->>WK: sections with heading path, page, tables
  WK->>CH: chunk by structure, SHA-256 per chunk, deterministic chunk ids
  WK->>DB: read CHUNK_LEDGER for document
  WK->>WK: diff into new, unchanged and removed hashes
  opt new chunks exist
    WK->>EM: GenerateAsync(new chunk texts, batched)
    WK->>IX: UpsertAsync(new chunks with tenant, ACL principals, acl_version)
  end
  opt removed chunks exist
    WK->>IX: DeleteChunksAsync(removed ids)
  end
  WK->>DB: update ledger, document Indexed, job Succeeded (one transaction)
  WK->>Q: CompleteAsync(receipt)
  alt exception and attempts below max
    WK->>DB: job Queued, last_error
    Note over WK,Q: message reappears after visibility timeout and is retried
  else attempts exhausted
    WK->>Q: DeadLetterAsync(receipt)
    WK->>DB: job DeadLettered, document Failed
  end
```

Re-running the same message is safe: chunk ids are deterministic, upserts
overwrite, and the ledger diff makes a second run embed zero chunks.

### 9.4 ACL change and delete (not hidden by content hashing)

```mermaid
sequenceDiagram
  autonumber
  actor Admin as Knowledge admin
  participant API as api
  participant DB as Metadata DB
  participant Q as IWorkQueue
  participant WK as worker
  participant IX as IIndexStore
  participant BL as IBlobStore

  Admin->>API: PUT /api/v1/documents/{id}/acl
  API->>DB: replace DOCUMENT_ACL, acl_version + 1
  API->>DB: job UpdateAcl (key docId:acl:aclVersion)
  API->>Q: enqueue
  Q-->>WK: UpdateAcl
  WK->>IX: UpdateAclAsync(doc, principals, acl_version)
  Note over WK,IX: Payload-only update, no re-embedding.<br/>Retriever also drops hits whose acl_version is older than the database (defence in depth).

  Admin->>API: DELETE /api/v1/documents/{id}
  API->>DB: document Deleting, job Delete
  API->>Q: enqueue
  Q-->>WK: Delete
  WK->>IX: DeleteByDocumentAsync(tenant, doc)
  WK->>BL: DeleteAsync(blob keys)
  WK->>DB: delete ledger rows, document Deleted
```

### 9.5 Login and token flow (Backend-for-Frontend)

```mermaid
sequenceDiagram
  autonumber
  actor U as User
  participant B as Browser
  participant WEB as web (Next.js BFF)
  participant IDP as Keycloak / Entra ID
  participant API as api

  U->>B: open app
  B->>WEB: GET /
  WEB-->>B: redirect to IdP (authorization code + PKCE)
  B->>IDP: login
  IDP-->>B: redirect with code
  B->>WEB: GET /auth/callback with code
  WEB->>IDP: exchange code for tokens
  WEB->>WEB: store tokens in encrypted, httpOnly, SameSite cookie session
  WEB-->>B: session cookie only (browser never sees the access token)
  B->>WEB: POST /api/chat
  WEB->>WEB: refresh access token if near expiry
  WEB->>API: forward with Authorization Bearer access_token
  API->>API: validate issuer, audience, signature (JWKS cache), expiry
  API->>API: map claims to Principal(tenant, user, groups)
  opt Entra group overage claim present
    API->>IDP: Microsoft Graph getMemberGroups (cached 10 min)
  end
```

### 9.6 Chat: retrieval, streaming and widget tools

```mermaid
sequenceDiagram
  autonumber
  actor U as User
  participant B as Browser useChat
  participant WEB as web BFF
  participant API as api ChatOrchestrator
  participant G as Guardrails
  participant R as HybridRetriever
  participant EM as IEmbeddingGenerator
  participant IX as IIndexStore
  participant RR as IReranker
  participant L as IChatClient
  participant DB as Metadata DB

  U->>B: ask "Compare revenue for FY2023 and FY2024"
  B->>WEB: POST /api/chat (UI messages)
  WEB->>API: POST /api/v1/chat (Bearer, traceparent)
  API->>API: principal, rate limit, conversation lookup
  API->>G: input checks (length, injection heuristics)
  API-->>WEB: SSE start, start-step
  API->>R: RetrieveAsync(query, principal)
  R->>EM: embed query
  par dense
    R->>IX: SearchDenseAsync(vector, aclFilter, 50)
  and sparse
    R->>IX: SearchSparseAsync(text, aclFilter, 50)
  end
  R->>R: ReciprocalRankFusion(k=60), keep top 30
  R->>RR: rerank 30 to top 8
  R-->>API: chunks with ids, scores, titles
  alt no chunk above relevance floor
    API-->>WEB: text "I could not find this in documents you can access."
  else context found
    API->>API: PromptBuilder fences each chunk as data with its chunk id
    API->>L: GetStreamingResponseAsync(messages, tools)
    loop streamed updates
      L-->>API: text update
      API-->>WEB: text-delta
      WEB-->>B: pass-through
    end
    opt model calls showFinancialChart
      L-->>API: function call + JSON args
      API->>G: validate args against contract schema and source ids against retrieved ids
      API-->>WEB: tool-input-available, tool-output-available
    end
    API->>G: extract cited chunk ids, drop citations not in retrieved set
    API-->>WEB: source-document parts for each valid citation
  end
  API-->>WEB: finish-step, finish, DONE marker
  API->>DB: save messages, USAGE_RECORD (tokens, cost, ttft)
  B->>B: render text, FinancialChart, Citation chips
```

### 9.7 Action approval (human in the loop, server-verified)

```mermaid
sequenceDiagram
  autonumber
  actor U as User
  participant B as Browser
  participant WEB as web BFF
  participant API as api
  participant L as IChatClient
  participant DB as Metadata DB
  participant ACT as ActionExecutor

  L-->>API: function call proposeAction(createFollowUpTask, args)
  API->>API: validate args, authorize tool for principal
  API->>DB: PENDING_APPROVAL(approvalId, toolCallId, user, args_sha256, expires in 10 min)
  API-->>WEB: tool-input-available, tool-approval-request(approvalId)
  API-->>WEB: finish (model turn pauses)
  B->>U: ActionCard with Approve and Deny
  U->>B: Approve
  B->>WEB: POST /api/chat (messages incl. approval response)
  WEB->>API: forward
  API->>DB: load approval by approvalId
  API->>API: check same user, status Pending, not expired, args hash unchanged
  alt valid and approved
    API->>ACT: execute createFollowUpTask(args)
    ACT->>DB: ACTION_RECORD, approval Executed
    API-->>WEB: tool-output-available(result)
    API->>L: continue turn with tool result
  else denied
    API->>DB: approval Denied
    API-->>WEB: tool-output-denied
    API->>L: continue turn, tool was denied
  else invalid, expired or tampered
    API-->>WEB: error part, approval rejected
  end
```

### 9.8 Evaluation run

```mermaid
sequenceDiagram
  autonumber
  participant CI as GitHub Actions
  participant AH as AppHost (test mode)
  participant SEED as seed tool
  participant EV as evals runner (Python)
  participant API as api
  participant J as Judge LLM (pinned)

  CI->>AH: start stack (profile under test)
  CI->>SEED: ingest eval corpus with persona ACLs
  SEED->>API: POST documents, poll until Indexed
  CI->>EV: uv run kc-eval --profile free --suite smoke
  loop each golden question
    EV->>API: POST /api/v1/answers (as persona)
    API-->>EV: answer, contexts, citations, usage
  end
  EV->>EV: retrieval metrics (recall@k, MRR, ACL leaks must be 0)
  EV->>J: RAGAS faithfulness, answer relevancy, context precision and recall
  EV->>EV: compare against thresholds for this profile
  EV-->>CI: results JSON + Markdown summary, exit 1 if below threshold
```

### 9.9 Azure deployment

```mermaid
sequenceDiagram
  autonumber
  actor Dev as Developer
  participant GH as GitHub Actions
  participant AAD as Entra ID (OIDC)
  participant ARM as Azure Resource Manager
  participant ACR as Container Registry
  participant ACA as Container Apps

  Dev->>GH: open PR touching infra/ or src/
  GH->>AAD: exchange GitHub OIDC token (federated credential)
  AAD-->>GH: short-lived Azure token
  GH->>ARM: az deployment group what-if (Bicep)
  GH-->>Dev: what-if diff posted to the job summary
  Dev->>GH: merge to main (environment approval)
  GH->>ACR: build and push images tagged with git SHA
  GH->>ARM: deploy Bicep (images by digest)
  ARM->>ACA: new revisions
  GH->>ACA: smoke test /health and /alive
  GH->>GH: eval smoke suite against azure profile
```

## 10. API surface

| Method & path                    | Auth                | Purpose                                         | Step |
| -------------------------------- | ------------------- | ----------------------------------------------- | ---- |
| `GET /alive`                     | anonymous           | Liveness: process is up                         | 0    |
| `GET /health`                    | anonymous           | Readiness: dependencies reachable               | 0    |
| `GET /openapi/v1.json`           | dev only            | OpenAPI 3.1 document                            | 1    |
| `POST /api/v1/documents`         | `kc.admin`          | Upload + ACL; 202 with job                      | 3    |
| `GET /api/v1/documents`          | user                | List documents the caller can see               | 3    |
| `GET /api/v1/documents/{id}`     | user (ACL)          | Status and metadata                             | 3    |
| `PUT /api/v1/documents/{id}/acl` | `kc.admin`          | Replace ACL; enqueues UpdateAcl                 | 3    |
| `DELETE /api/v1/documents/{id}`  | `kc.admin`          | Enqueues Delete                                 | 3    |
| `POST /api/v1/search`            | user                | Debug retrieval: ranked chunks (dev and evals) | 4    |
| `POST /api/v1/chat`              | user                | SSE, AI SDK UI message stream v1                | 5    |
| `POST /api/v1/answers`           | user / eval persona | Non-streaming answer + contexts for evals       | 5    |

Errors use RFC 9457 `ProblemDetails`. All business endpoints are versioned
under `/api/v1`.

## 11. Security model

```mermaid
flowchart LR
  subgraph untrusted["Untrusted"]
    browser["Browser"]
    docsrc["Document content<br/>(may contain injections)"]
    model["LLM output"]
  end
  subgraph trusted["Trusted boundary"]
    bff["web BFF"]
    api["api"]
    worker["worker"]
    data[("DB · index · blobs")]
  end
  browser -->|"cookie session only"| bff
  bff -->|"Bearer access token"| api
  docsrc -->|"parsed as data"| worker
  model -->|"validated before use"| api
  api --> data
  worker --> data
```

| Threat                                  | Mitigation                                                                                                   | Step  |
| --------------------------------------- | ------------------------------------------------------------------------------------------------------------ | ----- |
| Cross-user or cross-tenant data leak    | ACL pre-filter in index query; tenant mandatory; ACL leak tests for every adapter; `acl_version` check        | 4, 8  |
| Stale permissions after ACL change      | Separate `UpdateAcl` job; `acl_version` compared at query time                                                | 3, 4  |
| Indirect prompt injection via documents | Context fenced as data with ids; system prompt says documents are untrusted; tools cannot exfiltrate; tests  | 5     |
| Forged or replayed approvals            | Server-side `PENDING_APPROVAL` bound to user, tool call, args hash, expiry; single use                        | 5, 7  |
| Hallucinated citations                  | Citations must reference retrieved chunk ids; others are dropped                                              | 5     |
| Token theft in the browser              | BFF pattern: httpOnly encrypted cookie; access token only server-side                                         | 8     |
| Dev auth shortcut reaching production   | `DevCurrentPrincipal` registered only in `Development`; startup fails otherwise                               | 2     |
| Semantic cache leak                     | Cache key includes tenant + sorted principal-set hash + model + prompt version                                | 12    |
| Cost or DoS abuse                       | Per-user and per-tenant rate limits, max tokens, upload size limits                                           | 12    |
| Secrets in repo or CI                   | Managed identity, Key Vault, GitHub OIDC; no keys in config; secret scanning                                  | 0, 11 |
| Malicious uploads                       | Content-type allow-list, size cap, parser isolated in its own container                                       | 3     |

## 12. Observability

- **Traces:** ASP.NET Core, HttpClient, EF Core, Azure SDK, and
  `Microsoft.Extensions.AI` `UseOpenTelemetry()` (GenAI semantic conventions
  such as `gen_ai.request.model` and `gen_ai.usage.input_tokens`). Trace
  context is carried through the queue in the message's `traceparent`.
- **Custom spans:** `retrieval.dense`, `retrieval.sparse`, `retrieval.fuse`,
  `retrieval.rerank`, `ingest.parse`, `ingest.chunk`, `ingest.embed`,
  `ingest.index`.
- **Metrics:** `kc.chat.ttft` (histogram, ms), `kc.retrieval.duration`,
  `kc.llm.cost` (USD counter by model and tenant), `kc.ingest.chunks`
  (embedded vs skipped), `kc.queue.age`.
- **Every signal** carries `app.profile`, `deployment.environment` and
  `service.version`.
- **Exporters:** OTLP → Aspire dashboard (always in dev) and optionally Phoenix;
  Azure Monitor distro in `azure`.
- **Prompt and completion content** is not recorded by default; enable it only
  in dev via `KnowledgeCopilot:Telemetry:CaptureContent=true`.

## 13. Configuration

Options classes are bound from configuration sections and validated with
`ValidateOnStart()`, so a misconfigured profile fails at boot rather than on the
first request.

| Section                      | Keys (examples)                                                          | Profiles |
| ---------------------------- | ------------------------------------------------------------------------ | -------- |
| `KnowledgeCopilot`           | `Profile` (`Free` / `Azure`)                                             | both     |
| `KnowledgeCopilot:Llm`       | `ChatModel`, `EmbeddingModel`, `EmbeddingDimensions`, `MaxOutputTokens`  | both     |
| `KnowledgeCopilot:Retrieval` | `DenseK`, `SparseK`, `RrfK`, `RerankTopN`, `MinRelevance`                | both     |
| `KnowledgeCopilot:Ingestion` | `MaxUploadMb`, `ChunkTargetTokens`, `ChunkOverlapTokens`, `MaxAttempts`  | both     |
| `KnowledgeCopilot:Auth`      | `Authority`, `Audience`, `TenantClaim`, `GroupsClaim`                    | both     |
| `KnowledgeCopilot:Pricing`   | per-model USD per 1M input and output tokens                             | both     |
| `ConnectionStrings:*`        | injected by Aspire (`kcdb`, `blobs`, `queues`, `qdrant`, …)              | both     |

Locally, secrets go in user secrets or Aspire parameters. In Azure, apps use
managed identity, so no connection string contains a key.

## 14. Deployment

| Concern      | `Free`                                                                 | `Azure`                                                         |
| ------------ | ---------------------------------------------------------------------- | --------------------------------------------------------------- |
| Dev loop     | `aspire run` (AppHost starts every container)                          | `aspire run` with `profile=azure` against a dev resource group |
| Packaging    | .NET SDK container publish (no Dockerfile) + Next.js Dockerfile        | same images                                                     |
| Run anywhere | Docker Compose generated by Aspire's Docker publisher, then committed | Container Apps via Bicep in `infra/`                            |
| CI/CD        | CI builds images and runs the eval smoke suite                         | GitHub OIDC → what-if on PR → deploy on main → smoke + eval     |

## 15. Initial SLOs and budgets

Initial targets, revisited with load-test data in Step 12.

| Metric                                  | `Free` (laptop) | `Azure`                      |
| --------------------------------------- | --------------- | ---------------------------- |
| Time to first token p95                 | < 6 s           | < 2.5 s                      |
| Retrieval (embed + search + rerank) p95 | < 800 ms        | < 500 ms                     |
| Ingestion, 100-page Markdown document   | < 60 s          | < 60 s                       |
| Cost per answered question              | $0              | < $0.002                     |
| Monthly spend                           | $0              | $10–25 (budget alert at $20) |
| ACL leaks in eval suite                 | 0 (hard gate)   | 0 (hard gate)                |
