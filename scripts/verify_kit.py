#!/usr/bin/env python3
"""Read-only, dependency-free validation for the Codex spec pack."""
from pathlib import Path
import hashlib, json, re, sys

ROOT = Path(__file__).resolve().parents[1]
EXPECTED = [
    'AGENTS.md', 'README.md', 'START_HERE.md', 'CODEX_START_PROMPT.md',
    'VERIFICATION.md', 'CHANGELOG.md', 'research.md',
    'docs/research.md', 'docs/MASTER_SPEC.md', 'docs/IMPLEMENTATION_RFC.md',
    'docs/DESIGN_SYSTEM.md', 'docs/FEATURE_SCOPE.md', 'docs/GAP_AUDIT.md',
    'docs/ACTION_PLAN.md', 'docs/CODEX_PHASES.md', 'docs/QA_MATRIX.md',
    'docs/COMPATIBILITY_MATRIX.md', 'docs/DECISIONS.md', 'docs/TEST_RESULTS.md',
    'design/DESIGN_TOKENS.json', 'design/UX_CONCEPT.html',
    'design/UX_CONCEPT.png', 'design/UX_CONCEPT_GHOST.png',
    'scripts/Preflight-Windows.ps1', 'scripts/Sync-Research.ps1',
]
errors = []
for rel in EXPECTED:
    p = ROOT / rel
    if not p.is_file() or p.stat().st_size == 0:
        errors.append(f'missing/empty: {rel}')
try:
    data = json.loads((ROOT / 'design/DESIGN_TOKENS.json').read_text(encoding='utf-8'))
    assert isinstance(data, dict)
except Exception as e:
    errors.append(f'design tokens JSON: {e}')
if (ROOT / 'research.md').is_file() and (ROOT / 'docs/research.md').is_file():
    for_shas = [hashlib.sha256((ROOT / x).read_bytes()).hexdigest() for x in ('research.md','docs/research.md')]
    if for_shas[0] != for_shas[1]:
        errors.append('Research mirror out of sync: run scripts/Sync-Research.ps1 on Windows')
for p in list(ROOT.glob('*.md')) + list((ROOT/'docs').glob('*.md')):
    content = p.read_text(encoding='utf-8')
    if content.count('```') % 2 != 0:
        errors.append(f'unclosed code fence: {p.relative_to(ROOT)}')
    for target in re.findall(r'\[[^\]]+\]\(([^)]+)\)', content):
        target = target.split('#', 1)[0].strip()
        if not target or '://' in target or target.startswith('mailto:') or target.startswith('#'):
            continue
        if not (p.parent / target).exists():
            errors.append(f'broken local link in {p.relative_to(ROOT)}: {target}')
if errors:
    print('FAILED:')
    for e in errors: print('  -', e)
    sys.exit(1)
print(f'PASS: {len(EXPECTED)} expected spec files, tokens JSON, links, fence checks and research mirror.')
print('Note: Native Windows execution is not verified by this script.')
