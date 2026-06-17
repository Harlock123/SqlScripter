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
            BuildTree(objects);
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
        var selected = RootNodes.SelectMany(n => n.GetCheckedObjects()).ToList();
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

    [RelayCommand]
    private void SelectAll() => SetAllChecked(true);

    [RelayCommand]
    private void ClearSelection() => SetAllChecked(false);

    private void SetAllChecked(bool value)
    {
        foreach (var category in RootNodes)
            category.IsChecked = value;
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
                categoryNode.Children.Add(new DbObjectNode(obj.Display, obj, categoryNode));
            RootNodes.Add(categoryNode);
        }
    }
}
