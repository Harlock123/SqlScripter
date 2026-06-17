using Avalonia.Controls;
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
}
