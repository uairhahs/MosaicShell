---
applyTo: "**"
---

## Use Codebase Memory MCP to read the codebase

Applies to every request that involves this codebase.

### Rules

1. Call `list_projects` first to discover the correct project identifier. Never guess it.
2. Call `get_architecture(project)` next, before writing code, editing files, or answering a
   question about structure.
3. Use the returned context to make targeted changes.
4. Prefer the graph over `grep_search` / `file_search` / `semantic_search` / `read_file` for initial
   exploration. Fall back to those for string literals, config values, non-code files, and to verify
   anything the graph asserted.
5. Re-query as needed during implementation.

Tool names here are the server's own names. The client prefixes them, and the prefix depends on how
the server is registered locally, so never hardcode the prefixed form.

### Workflow

```text
list_projects()
get_architecture({ "project": "<name from list_projects>" })
search_graph({ "project": "<name>", "name_pattern": "<symbol>" })
trace_call_path({ "project": "<name>", "function_name": "<symbol>", "direction": "inbound" })
get_code_snippet({ "project": "<name>", "qualified_name": "<qualified name from search_graph>" })
check_index_coverage({ "project": "<name>", "paths": ["<file>"] })
```

### Why

- Pre-built index covers the whole codebase with relevance ranking.
- Faster and more accurate than manual file search.
- Does not follow stale files or ghost references.
- `check_index_coverage` is required before any negative or exhaustive claim. A clean result means
  no recorded gap, not proof of completeness.
