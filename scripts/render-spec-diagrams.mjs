// Render only authored diagram text in headless Edge; never captures the desktop.
import { createServer } from 'node:http';
import { readFile, readdir, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const { chromium } = await import(pathToFileURL(path.join(root, '.spec-tools/node_modules/playwright-core/index.mjs')).href);
const dist = path.join(root, '.spec-tools/node_modules/mermaid/dist');
const output = path.join(root, 'artifacts/spec-diagrams');
await mkdir(output, { recursive: true });
const server = createServer(async (req, res) => {
  try {
    const url = new URL(req.url, 'http://127.0.0.1');
    if (url.pathname === '/') {
      res.setHeader('Content-Type', 'text/html');
      res.end('<!doctype html><html><head><meta charset="utf-8"><style>body{margin:0;background:#fff;font-family:Segoe UI,sans-serif}#diagram{padding:24px;width:1752px}svg{display:block;margin:auto}</style></head><body><div id="diagram"></div></body></html>');
      return;
    }
    const full = path.resolve(dist, '.' + decodeURIComponent(url.pathname));
    if (!full.startsWith(dist + path.sep)) { res.writeHead(403); res.end(); return; }
    res.setHeader('Content-Type', 'text/javascript');
    res.end(await readFile(full));
  } catch { res.writeHead(404); res.end(); }
});
await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
let browser;
try {
  browser = await chromium.launch({ channel: 'msedge', headless: true });
  const page = await browser.newPage({ viewport: { width: 1800, height: 1200 }, deviceScaleFactor: 1 });
  const base = `http://127.0.0.1:${server.address().port}`;
  await page.goto(base);
  // Refuse all non-local network traffic during documentation rendering.
  await page.route('**/*', route => new URL(route.request().url()).origin === base ? route.continue() : route.abort());
  await page.evaluate(async () => {
    window.diagramMermaid = (await import('/mermaid.esm.min.mjs')).default;
    window.diagramMermaid.initialize({ startOnLoad: false, securityLevel: 'strict', theme: 'neutral', fontFamily: 'Segoe UI', flowchart: { useMaxWidth: true } });
  });
  const dir = path.join(root, 'docs/diagrams');
  const jobs = [];
  for (const name of (await readdir(dir)).filter(n => n.endsWith('.mmd')).sort()) jobs.push({ name: name.slice(0,-4), source: await readFile(path.join(dir,name),'utf8') });
  const directive = await readFile(path.join(root,'codex/MASTER-IMPLEMENTATION-PROMPT.md'),'utf8');
  [...directive.matchAll(/```mermaid\r?\n([\s\S]*?)```/g)].forEach((m,i)=>jobs.push({name:`directive-inline-${i+1}`,source:m[1]}));
  for (let i=0;i<jobs.length;i++) {
    const job=jobs[i];
    const svg = await page.evaluate(async ({ source, id }) => {
      const rendered = await window.diagramMermaid.render(id, source);
      document.querySelector('#diagram').innerHTML = rendered.svg;
      return rendered.svg;
    }, { source: job.source, id: `ghostDiagram${i}` });
    await writeFile(path.join(output,job.name+'.svg'),svg);
    await page.locator('#diagram').screenshot({path:path.join(output,job.name+'.png')});
  }
  console.log(`PASS: rendered ${jobs.length} diagrams as SVG and PNG using headless installed Edge. Output: ${output}. No desktop, camera or app capture.`);
} finally {
  await browser?.close();
  await new Promise(resolve=>server.close(resolve));
}
