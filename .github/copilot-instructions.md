## Codebase Memory MCP

**Use the codebase-memory knowledge graph FIRST for code discovery, before reading files or making changes.**

Applies to every request involving this codebase. The graph is a pre-built index with relevance
ranking: faster and more accurate than manual file search, and it does not follow stale references.

Tool names below are the server's own names. The client adds a prefix that depends on how the server
is registered locally, so never hardcode the prefixed form.

### Workflow

1. `list_projects` to discover the project identifier. Never guess it.
2. `get_architecture(project)` for structure, packages, hotspots, clusters.
3. `search_graph` for symbols, `trace_call_path` for call chains, `query_graph` for anything
   relational (complexity, duplicates, fan-in).
4. `get_code_snippet(qualified_name)` to read an implementation.
5. `check_index_coverage` before any negative or exhaustive claim. A clean result means no recorded
   gap, not proof of completeness.
6. `read_file` only when you need exact raw content to edit a specific line.

### Fall back to grep and glob for

String literals, error messages, config values, non-code files (`*.ps1`, `*.iss`, `*.axaml`,
workflow YAML), and verification of anything the graph asserted.

### Known index gap in this repo

A small number of Core `.cs` files have symbol nodes but no `File` node, so file-level queries can
undercount. Symbol-level queries are unaffected. Verify file counts with a file search.

### Entry points worth knowing

- Flyout pipeline: `TesseraCapability` -> `CapabilityFlyoutSession.Route` -> `IFlyoutPresenter` ->
  `AvaloniaFlyoutPresenter` -> `FlyoutWindow`.
- Per-style facts: `TesseraFlyoutTweenTargetCatalog.ResolveProfile` is the only `switch (styleId)`.
- Parity honesty flags: `host/MosaicShell.Core.Tests/HubParityBacklogTests.cs`.
