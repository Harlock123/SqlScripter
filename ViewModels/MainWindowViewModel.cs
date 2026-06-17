using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlScripter.Models;
using SqlScripter.Services;

namespace SqlScripter.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly DatabaseService _database = new();
    private readonly SqlScriptGenerator _generator = new();

    /// <summary>Every object returned by the last successful connect (unfiltered).</summary>
    private IReadOnlyList<DbObjectInfo> _allObjects = Array.Empty<DbObjectInfo>();

    /// <summary>Keys of objects the user has ticked, kept across filter changes.</summary>
    private readonly HashSet<string> _selectedKeys = new();

    [ObservableProperty]
    private string _connectionString =
        "Server=localhost,1433;Database=YourDatabase;User Id=sa;Password=Your_password123;TrustServerCertificate=True;Encrypt=True;";

    [ObservableProperty]
    private string _generatedSql = "-- Connect to a database and select objects, then click \"Generate Script\".";

    [ObservableProperty]
    private string _statusMessage = "Ready.";

    /// <summary>Emit existence checks + DROP before each recreated object.</summary>
    [ObservableProperty]
    private bool _scriptChecksAndDrops = true;

    /// <summary>Emit INSERT statements reproducing the data of scripted tables.</summary>
    [ObservableProperty]
    private bool _scriptInsertData;

    /// <summary>Per-table row cap for INSERT scripting. 0 means unlimited.</summary>
    [ObservableProperty]
    private int _maxDataRows = 1000;

    /// <summary>Substring used to filter the object tree (case-insensitive).</summary>
    [ObservableProperty]
    private string _filterText = string.Empty;

    /// <summary>0 = show items that contain the pattern, 1 = show items that do not.</summary>
    [ObservableProperty]
    private int _filterModeIndex;

    partial void OnFilterTextChanged(string value) => RebuildTree();
    partial void OnFilterModeIndexChanged(int value) => RebuildTree();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateScriptCommand))]
    private bool _isBusy;

    public ObservableCollection<DbObjectNode> RootNodes { get; } = new();

    private bool NotBusy => !IsBusy;

    [RelayCommand(CanExecute = nameof(NotBusy))]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            StatusMessage = "Please enter a connection string.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Connecting…";
        RootNodes.Clear();

        try
        {
            var objects = await _database.GetObjectsAsync(ConnectionString);
            _allObjects = objects;
            _selectedKeys.Clear();
            RebuildTree();
            StatusMessage = $"Connected. Found {objects.Count} object(s): " +
                            $"{objects.Count(o => o.Type == SqlObjectType.Table)} tables, " +
                            $"{objects.Count(o => o.Type == SqlObjectType.View)} views, " +
                            $"{objects.Count(o => o.Type == SqlObjectType.StoredProcedure)} procedures.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(NotBusy))]
    private async Task GenerateScriptAsync()
    {
        // Use the tracked selection so objects ticked then hidden by a filter are
        // still scripted. Preserve the original server ordering.
        var selected = _allObjects.Where(o => _selectedKeys.Contains(Key(o))).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "Select one or more objects in the tree first.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Generating script for {selected.Count} object(s)…";

        try
        {
            var options = new ScriptOptions
            {
                IncludeDropChecks = ScriptChecksAndDrops,
                IncludeTableData = ScriptInsertData,
                MaxDataRows = MaxDataRows
            };
            GeneratedSql = await _generator.GenerateAsync(ConnectionString, selected, options);
            StatusMessage = $"Generated script for {selected.Count} object(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Script generation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Clears the generated SQL output pane.
    [RelayCommand]
    private void ClearOutput()
    {
        GeneratedSql = string.Empty;
        StatusMessage = "Output cleared.";
    }

    // "All" selects every currently visible (filtered) object.
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var category in RootNodes)
            category.IsChecked = true;
    }

    // "None" clears the visible ticks and any selection hidden by the filter.
    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var category in RootNodes)
            category.IsChecked = false;
        _selectedKeys.Clear();
    }

    /// <summary>Re-applies the current filter to <see cref="_allObjects"/> and rebuilds the tree.</summary>
    private void RebuildTree()
    {
        var pattern = FilterText?.Trim() ?? string.Empty;
        IEnumerable<DbObjectInfo> filtered = _allObjects;

        if (pattern.Length > 0)
        {
            var exclude = FilterModeIndex == 1; // 1 = "does not contain"
            filtered = _allObjects.Where(o =>
            {
                var matches = o.Display.Contains(pattern, StringComparison.OrdinalIgnoreCase);
                return exclude ? !matches : matches;
            });
        }

        BuildTree(filtered.ToList());
    }

    private void BuildTree(IReadOnlyList<DbObjectInfo> objects)
    {
        RootNodes.Clear();

        var categories = new (string Title, SqlObjectType Type)[]
        {
            ("Tables", SqlObjectType.Table),
            ("Views", SqlObjectType.View),
            ("Stored Procedures", SqlObjectType.StoredProcedure)
        };

        foreach (var (title, type) in categories)
        {
            var members = objects.Where(o => o.Type == type).ToList();
            var categoryNode = new DbObjectNode($"{title} ({members.Count})");
            foreach (var obj in members)
            {
                var leaf = new DbObjectNode(obj.Display, obj, categoryNode)
                {
                    CheckChanged = OnNodeCheckChanged
                };
                // Restore the tick if this object was selected before re-filtering.
                if (_selectedKeys.Contains(Key(obj)))
                    leaf.IsChecked = true;
                categoryNode.Children.Add(leaf);
            }
            RootNodes.Add(categoryNode);
        }
    }

    private void OnNodeCheckChanged(DbObjectNode node)
    {
        if (node.Info is null)
            return; // category nodes aren't tracked directly

        var key = Key(node.Info);
        if (node.IsChecked)
            _selectedKeys.Add(key);
        else
            _selectedKeys.Remove(key);
    }

    private static string Key(DbObjectInfo o) => $"{o.Type}:{o.Schema}.{o.Name}";
}
