// Generates web/src/contracts/{api.d.ts,zod.ts} from contracts/schema/openapi.json (D-1-01).
// Run through `make contracts`; never edit the output by hand.
import { mkdtemp, readFile, rm, writeFile, mkdir } from "node:fs/promises";
import { tmpdir } from "node:os";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import openapiTS, { astToString } from "openapi-typescript";
import { createClient } from "@hey-api/openapi-ts";

const webDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const input = path.resolve(webDir, "../contracts/schema/openapi.json");
const outDir = path.join(webDir, "src/contracts");
const header = "// GENERATED - DO NOT EDIT. Run `make contracts`.\n// Source: contracts/schema/openapi.json\n\n";

const toLf = (text) => text.replace(/\r\n/g, "\n").replace(/\n*$/, "\n");

async function write(name, body) {
  await writeFile(path.join(outDir, name), header + toLf(body), "utf8");
}

await mkdir(outDir, { recursive: true });

const ast = await openapiTS(pathToFileURL(input));
await write("api.d.ts", astToString(ast));

// hey-api cleans its output folder, so generate into a temp folder and copy the zod file out.
const temp = await mkdtemp(path.join(tmpdir(), "kc-zod-"));
try {
  await createClient({
    input,
    output: { path: temp, clean: true },
    plugins: [{ name: "zod", dates: { offset: true } }],
    logs: { level: "silent" },
  });
  await write("zod.ts", await readFile(path.join(temp, "zod.gen.ts"), "utf8"));
} finally {
  await rm(temp, { recursive: true, force: true });
}
