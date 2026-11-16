using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using AvaloniaEdit.TextMate;
using FluentAvalonia.UI.Windowing;
using OwlScripts.BgymlKeyManager.Models;
using OwlScripts.BgymlKeyManager.ViewModels;
using TextMateSharp.Grammars;

namespace OwlScripts.BgymlKeyManager;

public partial class MainWindow : FAAppWindow
{
    private readonly MainViewModel _vm;
    
    public MainWindow()
    {
        DataContext = _vm = new MainViewModel();
        InitializeComponent();
        
        Bitmap bitmap = new(AssetLoader.Open(new Uri($"avares://{typeof(App).Namespace}/Assets/Icon.png")));
        Icon = bitmap.CreateScaledBitmap(new PixelSize(48, 48));
        
        DragDrop.AddDropHandler(DropTarget, OnDrop);
        MainTreeView.AddHandler(KeyDownEvent, TreeView_OnKeyDown, handledEventsToo: true);

        var theme = Application.Current?.ActualThemeVariant == ThemeVariant.Light ? ThemeName.LightPlus : ThemeName.DarkPlus;
        BgymlPreview.InstallTextMate(new RegistryOptions(theme)).SetGrammar("source.yaml");

        _vm.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_vm.CurrentBgyml)) {
                BgymlPreview.Text = _vm.CurrentBgyml;
            }
        };
    }

    private void OnDrop(object? s, DragEventArgs e)
    {
        if (e.DataTransfer.TryGetFile() is not IStorageFile file || !file.Name.EndsWith(".json")) {
            Instructions_Label.Text = "Invalid file(s)";
            return;
        }

        Instructions_Label.IsVisible = false;
        EditorContent.IsVisible = true;
        _vm.LoadCommand.Execute(file.Path.AbsolutePath);
    }

    private void TreeView_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && sender is TreeView { SelectedItem: BgymlEntity entity }) {
            entity.Include = !entity.Include;
        }
    }
}