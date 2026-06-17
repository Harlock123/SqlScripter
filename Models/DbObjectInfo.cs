namespace SqlScripter.Models;

/// <summary>The kinds of database objects this tool can enumerate and script.</summary>
public enum SqlObjectType
{
    Table,
    View,
    StoredProcedure
}

/// <summary>A single schema-qualified database object discovered on the server.</summary>
public sealed class DbObjectInfo
{
    public required string Schema { get; init; }
    public required string Name { get; init; }
    public required SqlObjectType Type { get; init; }

    /// <summary>Bracket-quoted, schema-qualified name, e.g. <c>[dbo].[Customer]</c>.</summary>
    public string FullName => $"[{Schema}].[{Name}]";

    /// <summary>Friendly name for display in the tree, e.g. <c>dbo.Customer</c>.</summary>
    public string Display => $"{Schema}.{Name}";
}
