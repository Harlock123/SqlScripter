using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SqlScripter.Models;

namespace SqlScripter.ViewModels;

/// <summary>
/// A node in the object tree. A node is either a category header
/// (Tables / Views / Stored Procedures), in which case <see cref="Info"/> is null
/// and it owns child nodes, or a concrete object leaf.
/// Checking a category checks every child, giving a simple multi-select.
/// </summary>
public partial class DbObjectNode : ObservableObject
{
    private readonly DbObjectNode? _parent;
    private bool _suppressPropagation;

    public DbObjectNode(string title, DbObjectInfo? info = null, DbObjectNode? parent = null)
    {
        Title = title;
        Info = info;
        _parent = parent;
    }

    public string Title { get; }
    public DbObjectInfo? Info { get; }
    public ObservableCollection<DbObjectNode> Children { get; } = new();

    public bool IsCategory => Info is null;

    /// <summary>
    /// Raised whenever this node's checked state changes (including cascades from a
    /// category). The view model uses it to track selection independently of the
    /// currently visible/filtered tree.
    /// </summary>
    public Action<DbObjectNode>? CheckChanged { get; set; }

    [ObservableProperty]
    private bool _isChecked;

    [ObservableProperty]
    private bool _isExpanded = true;

    partial void OnIsCheckedChanged(bool value)
    {
        // Checking/unchecking a category cascades to all of its children.
        if (!_suppressPropagation)
        {
            foreach (var child in Children)
            {
                child._suppressPropagation = true;
                child.IsChecked = value;
                child._suppressPropagation = false;
            }
        }

        CheckChanged?.Invoke(this);
    }

    /// <summary>Yields every checked object leaf at or beneath this node.</summary>
    public IEnumerable<DbObjectInfo> GetCheckedObjects()
    {
        if (Info is not null && IsChecked)
            yield return Info;

        foreach (var child in Children)
            foreach (var info in child.GetCheckedObjects())
                yield return info;
    }
}
