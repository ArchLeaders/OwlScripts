using CommunityToolkit.Mvvm.ComponentModel;

namespace OwlScripts.BgymlKeyManager.Models;

public abstract partial class BgymlEntity : ObservableObject
{
    [ObservableProperty]
    public required partial string Name { get; set; }

    [ObservableProperty]
    public partial bool Include { get; set; } = true;

    public BgymlEntity? Parent { get; init; }
}