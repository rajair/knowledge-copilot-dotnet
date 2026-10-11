# Single entry point for humans and CI (D-18). Recipes call dotnet, pnpm and uv directly and
# avoid shell-specific syntax so they run under GNU make on Windows and Linux.

SOLUTION := KnowledgeCopilot.slnx
APPHOST  := src/KnowledgeCopilot.AppHost/KnowledgeCopilot.AppHost.csproj

.DEFAULT_GOAL := help

.PHONY: help setup build lint test dev clean \
	setup-dotnet setup-web setup-evals \
	build-dotnet build-web \
	lint-dotnet lint-web lint-evals \
	test-dotnet test-web test-evals

help:
	$(info Targets: setup build lint test dev clean)
	$(info Per part: setup-, build-, lint-, test- with suffix -dotnet, -web or -evals (no build-evals))

setup: setup-dotnet setup-web setup-evals

setup-dotnet:
	dotnet restore $(SOLUTION)

setup-web:
	pnpm -C web install --frozen-lockfile

setup-evals:
	uv sync --project evals --locked

build: build-dotnet build-web

build-dotnet:
	dotnet build $(SOLUTION) --no-restore

build-web:
	pnpm -C web build

lint: lint-dotnet lint-web lint-evals

lint-dotnet:
	dotnet format $(SOLUTION) --verify-no-changes

lint-web:
	pnpm -C web lint

lint-evals:
	uv run --project evals --locked ruff check evals
	uv run --project evals --locked ruff format --check evals

test: test-dotnet test-web test-evals

test-dotnet:
	dotnet test --solution $(SOLUTION)

test-web:
	pnpm -C web test

test-evals:
	uv run --project evals --locked pytest evals

# Prefer the Aspire CLI; fall back to running the AppHost directly when the CLI is not installed.
dev:
	aspire run --apphost $(APPHOST) || dotnet run --project $(APPHOST)

# Removes build output and installed dependencies (git-ignored files only).
clean:
	dotnet clean $(SOLUTION)
	git clean -fdX -- src tests web evals
