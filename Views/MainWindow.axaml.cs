using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SqlScripter.ViewModels;
using SyntaxColorizer.Themes;

namespace SqlScripter.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // The control's Language is set to MsSql in XAML; here we just pick a
        // syntax colour theme so the generated SQL is highlighted like a code editor.
        SqlEditor.SyntaxTheme = BuiltInThemes.VisualStudioDark;
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm)
            return;

        var clipboard = Clipboard;
        if (clipboard is null)
        {
            vm.StatusMessage = "Clipboard is not available on this platform.";
            return;
        }

        await clipboard.SetTextAsync(vm.GeneratedSql ?? string.Empty);
        vm.StatusMessage = "Script copied to clipboard.";
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm)
            return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save SQL script",
            SuggestedFileName = "script.sql",
            DefaultExtension = "sql",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("SQL script") { Patterns = new[] { "*.sql" } },
                FilePickerFileTypes.All
            }
        });

        if (file is null)
        {
            vm.StatusMessage = "Save cancelled.";
            return;
        }

        try
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(vm.GeneratedSql ?? string.Empty);
            vm.StatusMessage = $"Saved script to {file.Name}.";
        }
        catch (Exception ex)
        {
            vm.StatusMessage = $"Save failed: {ex.Message}";
        }
    }
}
