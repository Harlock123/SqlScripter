# SqlScripter

A cross-platform desktop tool for **Microsoft SQL Server** that lets you point at a
database with a connection string, browse its **tables, views and stored procedures**
in a tree, tick the ones you want, and generate a **"drop &amp; recreate" T-SQL script**
into a SQL-syntax-highlighted editor.

Built with **.NET 9** + **Avalonia 11.3** and the
[`SyntaxColorizer`](https://www.nuget.org/packages/SyntaxColorizer/) editor control.
Runs on **Windows, macOS and Linux** — Intel/AMD (x64) and ARM64.

## What it generates

For every selected object the script contains:

1. An **existence check** that `DROP`s the object if it is already present
   (`IF OBJECT_ID(N'[schema].[name]', N'U'/'V'/'P') IS NOT NULL ...`).
2. A full **recreation**:
   - **Tables** → `CREATE TABLE` with current data types, identity, computed
     columns, defaults, then `PRIMARY KEY` / `UNIQUE` / `FOREIGN KEY` / `CHECK`
     constraints and secondary indexes.
   - **Views** → the stored `CREATE VIEW` definition.
   - **Stored procedures** → the stored `CREATE PROCEDURE` definition.

The output is placed in the `SyntaxHighlightingTextBox` with its language set to
`MsSql`, so it is colourised as T-SQL and can be edited or copied out.

## Running from source

```bash
dotnet run
```

Enter a connection string such as:

```
Server=localhost,1433;Database=MyDb;User Id=sa;Password=Pass123;TrustServerCertificate=True;Encrypt=True;
```

Click **Connect** to populate the tree, tick objects (ticking a category selects all
of its children), then click **Generate Script**.

### Options

- **Script existence checks & DROP if present** — emit an `IF OBJECT_ID(...) ... DROP`
  guard before each recreated object.
- **Script INSERT records for tables** — also reproduce table data as `INSERT`
  statements, with a **Max rows per table** cap (`0` = all rows).

### Filtering the object list

Above the tree, the **filter** box narrows what's shown:

- Choose **Contains** or **Does not contain** and type a (case-insensitive) pattern,
  matched against each object's `schema.name`.
- Selections are remembered across filter changes — you can tick some items, change
  the filter, tick more, and **Generate Script** scripts everything you ticked (even
  rows currently hidden by the filter). **None** clears the entire selection.

## Building distributables for every platform

Each script publishes a self-contained, single-file native executable for all six
targets. Use the one that matches your OS — neither needs anything beyond a shell and
the .NET SDK:

**macOS / Linux** (bash, no PowerShell required):

```bash
./publish-all.sh                          # all targets -> ./publish/<rid>/
./publish-all.sh ./dist "win-x64,linux-x64"   # custom output + subset
```

**Windows** (PowerShell):

```pwsh
./publish-all.ps1
./publish-all.ps1 -OutRoot ./dist -Rids win-x64,linux-x64
```

…or one target at a time without a script:

```bash
dotnet publish SqlScripter.csproj -c Release -r win-arm64 \
  --self-contained true -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

Supported runtime identifiers: `win-x64`, `win-arm64`, `osx-x64`, `osx-arm64`,
`linux-x64`, `linux-arm64`. Each produces one standalone executable (no .NET install
required on the target). Artifacts land in `publish/<rid>/`.

## Notes

- Connections use `Microsoft.Data.SqlClient`. SQL authentication and integrated/AAD
  auth are all supported via the connection string — the tool just passes it through.
- Encrypted modules (views/procs created `WITH ENCRYPTION`) cannot be scripted; the
  tool emits a comment for those.
- The Avalonia version is pinned to **11.3.x** because the `SyntaxColorizer` 1.0.2
  control is built against Avalonia 11, and mixing it with Avalonia 12 would break
  binary compatibility.
