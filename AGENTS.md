# AGENTS.md

Notes for anyone, human or AI agent, changing this code. `README.md` covers what the tool does, its options and
how to publish it; this file covers how the code is put together and the rules that keep it working.

## What this is

A desktop tool for Microsoft SQL Server, in C# on .NET 9 and Avalonia 11.3 (MVVM with CommunityToolkit.Mvvm).
It lists a database's tables, views and stored procedures in a tree, and generates a "drop and recreate" T-SQL
script for the ones you tick, optionally with `INSERT` statements for table data. It runs on Windows, macOS and
Linux.

## Commands

```bash
dotnet build SqlScripter.sln
dotnet run                                   # needs a reachable SQL Server to do anything useful
./publish-all.sh [outRoot] ["rid,rid"]       # self-contained single-file builds into publish/<rid>/
```

`publish-all.ps1` does the same on Windows. There are no tests yet.

## Layout

- `SqlScripter.csproj`: the only project (`net9.0`, nullable on, compiled bindings by default). It lists six
  runtime identifiers: win, osx and linux, each x64 and arm64.
- `Services/DatabaseService.cs`: tests a connection and lists user objects of type `U`, `V` and `P` from
  `sys.objects`.
- `Services/SqlScriptGenerator.cs`: builds the script.
  - Views and procedures replay their stored module text after an optional `OBJECT_ID` drop guard; encrypted
    modules get a comment instead.
  - Tables are rebuilt from the catalog: columns (types, identity, computed, defaults, collation), key, foreign
    key and check constraints, and indexes, then optional data as `INSERT`s.
- `Services/ScriptOptions.cs`: the switches (drop checks, table data, `MaxDataRows`, where 0 means all rows).
- `Models/DbObjectInfo.cs`: schema, name and type of one object.
- `ViewModels/MainWindowViewModel.cs`: connect, generate, filter (contains or does-not-contain on
  `schema.name`), select all and none. Ticks are kept in `_selectedKeys`, so they survive filter changes.
- `ViewModels/DbObjectNode.cs`: one tree node; ticking a category ticks its children.
- `Views/MainWindow.axaml(.cs)`: the window, with the `SyntaxColorizer` editor set to `MsSql`; the code-behind
  handles copying and saving the output.
- `tools/gen_usage_doc.py` and `make-usage-doc.sh`: rebuild `USAGEDOCUMENT_ForSQLSCRIPTER.DOCX` from screenshots,
  using python-docx in a local `.docvenv`.

## Rules that keep it working

- **Avalonia stays on 11.3.x.** `SyntaxColorizer` 1.0.2 is built against Avalonia 11; moving to Avalonia 12
  breaks it.
- **`InvariantGlobalization` must stay false.** `Microsoft.Data.SqlClient` throws in invariant mode.
- **Generate from the catalog, not from guesses.** All table scripting reads `sys.*` views over the one open
  `SqlConnection`; quote names in brackets, as `DbObjectInfo.FullName` does.
- **Keep connection strings out of the repo.** The tool passes whatever is typed straight to SqlClient and stores
  nothing.
- **The screenshot harness is missing.** `screenshots.sh` and `make-usage-doc.sh` run `tools/Screenshots`, and
  the `.csproj` excludes `tools/**` for it, but that project is not in the repository. Those scripts fail until
  it is added back.

## Making a change

- Check generated scripts against a scratch database: run the output and confirm the objects come back the same.
- Update `README.md` when options or the UI change.
