namespace SqlScripter.Services;

/// <summary>User-toggleable switches that control what the generated script contains.</summary>
public sealed class ScriptOptions
{
    /// <summary>
    /// When true, each object is preceded by an existence check that DROPs it if
    /// it already exists. When false, only the CREATE/recreation is emitted.
    /// </summary>
    public bool IncludeDropChecks { get; init; } = true;

    /// <summary>
    /// When true, tables are followed by INSERT statements that reproduce their
    /// current row data (with SET IDENTITY_INSERT handling where needed).
    /// </summary>
    public bool IncludeTableData { get; init; }

    /// <summary>
    /// Maximum number of rows to script per table when <see cref="IncludeTableData"/>
    /// is on. Zero (or negative) means no limit.
    /// </summary>
    public int MaxDataRows { get; init; } = 1000;
}
