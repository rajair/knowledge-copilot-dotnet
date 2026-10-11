"""Shared contract fixtures must round-trip through the generated Pydantic models."""

import json
from pathlib import Path

import pytest
from pydantic import BaseModel, RootModel, ValidationError

from knowledge_evals.contracts import models

FIXTURES = Path(__file__).resolve().parents[2] / "contracts" / "fixtures"

# Contracts that never reach Python (queue messages are .NET only).
DOTNET_ONLY = {"JobMessage"}

type Model = type[BaseModel] | type[RootModel]


def _load(kind: str) -> list[tuple[str, str]]:
    names = sorted(p.name for p in (FIXTURES / kind).glob("*.json"))
    return [(name, name.split(".")[0]) for name in names if name.split(".")[0] not in DOTNET_ONLY]


VALID = _load("valid")
INVALID = _load("invalid")


def _model_for(type_name: str) -> Model | None:
    return getattr(models, type_name, None)


def _read(kind: str, name: str) -> str:
    return (FIXTURES / kind / name).read_text(encoding="utf-8")


def test_fixtures_exist() -> None:
    assert VALID
    assert INVALID


@pytest.mark.parametrize(("name", "type_name"), VALID + INVALID)
def test_fixture_type_has_generated_model(name: str, type_name: str) -> None:
    assert _model_for(type_name) is not None, name


@pytest.mark.parametrize("type_name", sorted(DOTNET_ONLY))
def test_dotnet_only_contract_has_no_generated_model(type_name: str) -> None:
    assert _model_for(type_name) is None


@pytest.mark.parametrize(("name", "type_name"), VALID)
def test_valid_fixture_parses(name: str, type_name: str) -> None:
    model = _model_for(type_name)
    assert model is not None
    # JSON-mode strict validation: no coercion of quoted numbers, ISO strings still accepted for
    # dates and UUIDs, matching the server's NumberHandling.Strict.
    model.model_validate_json(_read("valid", name), strict=True)


@pytest.mark.parametrize(("name", "type_name"), INVALID)
def test_invalid_fixture_is_rejected(name: str, type_name: str) -> None:
    model = _model_for(type_name)
    assert model is not None
    with pytest.raises(ValidationError):
        model.model_validate_json(_read("invalid", name), strict=True)


def test_search_request_defaults_match_server() -> None:
    request = models.SearchRequest.model_validate(
        json.loads(_read("valid", "SearchRequest.defaults.json"))
    )
    assert request.topK == 8
    assert request.mode is not None
    assert request.mode.root == "hybrid"
