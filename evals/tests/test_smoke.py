import knowledge_evals


def test_package_imports_with_version() -> None:
    assert knowledge_evals.__version__ == "0.1.0"
