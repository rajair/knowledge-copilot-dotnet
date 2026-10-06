# knowledge-copilot-dotnet

A production-grade, **permission-aware RAG copilot** built with .NET 10 and
Aspire 13, a Next.js 16 generative UI, and a Python RAGAS evaluation harness.
Users ask questions over company documents and get streamed, cited answers built
only from documents they are allowed to read. Charts, tables and approval-gated
actions are rendered as UI widgets.

It is also a **learning project**: every part is built step by step with a
coding agent, from prompts that are written, explained and reviewed as part of
the repo, so you learn the system and prompt engineering together.

## Two profiles, one codebase

| | `Free` (local OSS, $0) | `Azure` (managed, ~$10–25/month) |
| --- | --- | --- |
| LLM + embeddings | Ollama | Azure OpenAI |
| Vector + keyword search | Qdrant (dense + sparse) | Azure AI Search (Free tier) |
| Reranker | TEI cross-encoder | TEI container on Container Apps |
| Metadata DB | PostgreSQL | Azure SQL (free offer) |
| Blob + queue | Azurite | Azure Storage |
| Parsing | Markdown built-in + docling-serve | Markdown built-in + Document Intelligence F0 |
| Identity | Keycloak | Entra ID |
| Run with | `make dev` (Aspire) or `docker compose` | Bicep + GitHub Actions to Container Apps |

The profile is a configuration switch; application code depends only on ports
(see [architecture §6](docs/architecture.md#6-ports-and-adapters)).

## Documentation

| | |
| --- | --- |
| [docs/README.md](docs/README.md) | Index and reading order |
| [docs/architecture.md](docs/architecture.md) | Full architecture with C4 views, data model and sequence diagrams |
| [docs/decisions.md](docs/decisions.md) | Architecture decision records |
| [docs/prompt-engineering.md](docs/prompt-engineering.md) | How to prompt coding agents: principles P1–P12, template, anti-patterns |
| [docs/steps/README.md](docs/steps/README.md) | Roadmap: 13 steps, each with a plan and a copy-paste prompt |

## Roadmap

| Step | Delivers | Prompt technique |
| ---- | -------- | ---------------- |
| [0](docs/steps/step-00-scaffold.md) | Scaffold, AppHost, CI, Makefile | Scoping by naming; non-goals |
| [1](docs/steps/step-01-contracts.md) | Shared contracts and codegen | Single source of truth |
| [2](docs/steps/step-02-ports-and-fakes.md) | Ports, fakes, profile wiring | Interface-first prompting |
| [3](docs/steps/step-03-ingestion.md) | Ingestion pipeline with ACLs | Invariants and failure scenarios |
| [4](docs/steps/step-04-retrieval.md) | Hybrid retrieval + rerank | Test oracles |
| [5](docs/steps/step-05-chat-api.md) | Streaming chat API, tools, approvals | Protocol-exact examples |
| [6](docs/steps/step-06-evals.md) | RAGAS eval harness and CI gate | Precise metric definitions |
| [7](docs/steps/step-07-generative-ui.md) | Next.js generative UI | Enumerating UI states |
| [8](docs/steps/step-08-auth.md) | Auth and multi-tenancy | Threat-model prompting |
| [9](docs/steps/step-09-observability.md) | OpenTelemetry, GenAI spans, cost | Naming conventions as constraints |
| [10](docs/steps/step-10-free-packaging.md) | Docker Compose packaging | Environment parity |
| [11](docs/steps/step-11-azure-deploy.md) | Azure deployment | Guardrails and human gates |
| [12](docs/steps/step-12-hardening.md) | Load tests, limits, safe caching | Measure first |

## Status

Planning complete; implementation starts at Step 0.

## Related

A .NET port of the Python plan in [rajair/knowledge-copilot](https://github.com/rajair/knowledge-copilot).

## Licence

See [LICENSE](LICENSE).
