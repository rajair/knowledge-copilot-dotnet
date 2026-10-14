// @vitest-environment node
import { readdirSync, readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import type { ZodType } from "zod";
import * as schemas from "../zod";

// Contracts that never reach the browser (queue messages are .NET only).
const DOTNET_ONLY = new Set(["JobMessage"]);

const fixturesDir = fileURLToPath(new URL("../../../../contracts/fixtures/", import.meta.url));

function load(kind: "valid" | "invalid") {
  const dir = path.join(fixturesDir, kind);
  return readdirSync(dir)
    .filter((name) => name.endsWith(".json"))
    .sort()
    .map((name) => ({
      name,
      type: name.split(".")[0],
      json: JSON.parse(readFileSync(path.join(dir, name), "utf8")) as unknown,
    }));
}

function schemaFor(type: string): ZodType | undefined {
  return (schemas as Record<string, ZodType | undefined>)[`z${type}`];
}

const valid = load("valid").filter((f) => !DOTNET_ONLY.has(f.type));
const invalid = load("invalid").filter((f) => !DOTNET_ONLY.has(f.type));

describe("contract fixtures (zod)", () => {
  it("has fixtures to check", () => {
    expect(valid.length).toBeGreaterThan(0);
    expect(invalid.length).toBeGreaterThan(0);
  });

  it.each([...valid, ...invalid].map((f) => [f.name, f.type] as const))(
    "%s has a generated schema",
    (_name, type) => {
      expect(schemaFor(type)).toBeDefined();
    },
  );

  it.each([...DOTNET_ONLY])("%s stays .NET-only (no generated schema)", (type) => {
    expect(schemaFor(type)).toBeUndefined();
  });

  it.each(valid.map((f) => [f.name, f] as const))("%s parses", (_name, fixture) => {
    const result = schemaFor(fixture.type)!.safeParse(fixture.json);
    expect(result.error).toBeUndefined();
  });

  it.each(invalid.map((f) => [f.name, f] as const))("%s is rejected", (_name, fixture) => {
    expect(schemaFor(fixture.type)!.safeParse(fixture.json).success).toBe(false);
  });
});
