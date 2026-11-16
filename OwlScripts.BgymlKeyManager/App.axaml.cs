using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OwlScripts.Common;

namespace OwlScripts.BgymlKeyManager;

public partial class App : Application
{
    public static readonly Romfs Romfs = new(Config.Shared.GameDumpFolderPath ?? string.Empty);
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}