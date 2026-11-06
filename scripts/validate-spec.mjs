// Documentation-only checks; does not build or execute the Windows app.
import { readFile, readdir, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const documents = [
  'codex/MASTER-IMPLEMENTATION-PROMPT.md', 'codex/HANDOFF.md',
  'codex/EXECUTION-PHASES.md', 'docs/SPECIFICATION_REVIEW.md',
  'docs/TRACEABILITY.md', 'docs/diagrams/README.md',
];
let links = 0;
const errors = [];
for (const relative of documents) {
  const full = path.join(root, relative);
  const content = await readFile(full, 'utf8');
  if ((content.match(/^```/gm) ?? []).length % 2) errors.push(`${relative}: unclosed fence`);
  for (const match of content.matchAll(/\[[^\]]+\]\(([^)]+)\)/g)) {
    const target = match[1].split('#')[0];
    if (!target || /^[a-z]+:/i.test(target)) continue;
    links++;
    try { await stat(path.resolve(path.dirname(full), target)); }
    catch { errors.push(`${relative}: broken link ${target}`); }
  }
}
const directive = await readFile(path.join(root, documents[0]), 'utf8');
const matrix = await readFile(path.join(root, 'docs/TRACEABILITY.md'), 'utf8');
for (let i = 1; i <= 19; i++) {
  const id = `F${String(i).padStart(2, '0')}`;
  if (!directive.includes(`### ${id} —`) || !directive.includes(`AC-${id}`)) errors.push(`Missing contract/acceptance ${id}`);
  if (!matrix.includes(`| ${id} |`)) errors.push(`Missing traceability ${id}`);
}
const report = await readFile(path.join(root, 'docs/SPECIFICATION_REVIEW.md'), 'utf8');
for (const letter of 'ABCDEFGHIJKLMN') if (!report.includes(`## PART ${letter} —`)) errors.push(`Missing report part ${letter}`);
const canonical = await readFile(path.join(root, 'docs/research.md'));
const mirror = await readFile(path.join(root, 'research.md'));
if (!canonical.equals(mirror)) errors.push('Research mirror is not byte-identical');
const parserPath = process.env.GHOST_MERMAID_MODULE || path.join(root, '.spec-tools/node_modules/mermaid/dist/mermaid.esm.mjs');
let mermaid;
try {
  const { JSDOM } = await import(pathToFileURL(path.join(root, '.spec-tools/node_modules/jsdom/lib/api.js')).href);
  const dom = new JSDOM('<!doctype html><html><body></body></html>');
  globalThis.window = dom.window;
  globalThis.document = dom.window.document;
  mermaid = (await import(pathToFileURL(parserPath).href)).default;
}
catch { throw new Error('Mermaid parser missing; install documented validation-only dependency, then rerun.'); }
mermaid.initialize({ startOnLoad: false, securityLevel: 'strict' });
const diagramDir = path.join(root, 'docs/diagrams');
const diagramNames = (await readdir(diagramDir)).filter(name => name.endsWith('.mmd')).sort();
if (diagramNames.length !== 13) errors.push(`Expected 13 diagrams, found ${diagramNames.length}`);
for (const name of diagramNames) {
  try { await mermaid.parse(await readFile(path.join(diagramDir, name), 'utf8')); }
  catch (error) { errors.push(`${name}: ${error.message}`); }
}
const inline = [...directive.matchAll(/```mermaid\r?\n([\s\S]*?)```/g)];
for (let i = 0; i < inline.length; i++) {
  try { await mermaid.parse(inline[i][1]); }
  catch (error) { errors.push(`Inline diagram ${i + 1}: ${error.message}`); }
}
if (errors.length) { console.error(errors.join('\n')); process.exitCode = 1; }
else console.log(`PASS: ${documents.length} documents, ${links} local links, 19 contracts/AC/traceability rows, 14 report parts, byte-identical research mirror, ${diagramNames.length} Mermaid sources and ${inline.length} inline diagrams parsed. Rendering, Windows app build/runtime and hardware tests NOT RUN.`);
