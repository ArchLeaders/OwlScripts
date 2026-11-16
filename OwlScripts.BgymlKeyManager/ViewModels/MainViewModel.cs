using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OwlScripts.BgymlKeyManager.Models;

namespace OwlScripts.BgymlKeyManager.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private static readonly Dictionary<string, string> _loadedBgymlFiles = [];

    [ObservableProperty]
    public partial ObservableCollection<BgymlType>? BgymlArrays { get; set; }

    [ObservableProperty]
    public partial string? CurrentBgyml { get; set; }

    [ObservableProperty]
    public partial BgymlType? CurrentBgymlType { get; set; }

    [ObservableProperty]
    public partial BgymlFile? CurrentBgymlFile { get; set; }

    [ObservableProperty]
    public partial BgymlEntity? CurrentSelection { get; set; }

    [RelayCommand]
    public async Task Load(string jsonFilePath)
    {
        await using var fs = File.OpenRead(jsonFilePath);
        var arrays = await JsonSerializer.DeserializeAsync<Dictionary<string, BgymlEvaluationResult>>(fs) ?? [];

        BgymlArrays = [];

        foreach (var (key, value) in arrays) {
            var type = new BgymlType {
                Name = key,
                Files = [
                    .. value.Files
                        .Where(kvp => kvp.Value is not null && File.Exists(kvp.Value))
                        .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                        .Select(kvp => new BgymlFile(kvp.Key, kvp.Value!))
                ]
            };

            type.Arrays = [
                .. value.Arrays.Select(array => new BgymlArray {
                    Name = array.Key,
                    Parent = type,
                    PossibleKeys = [
                        .. array.Value.Select(propertyName => new BgymlArrayKey {
                            Name = propertyName,
                            Parent = type
                        })
                    ]
                })
            ];

            BgymlArrays.Add(type);
        }
    }

    [RelayCommand]
    private void PreviousFile()
    {
        CurrentBgymlFile = CurrentBgymlType?.GetFileAfter(CurrentBgymlFile);
    }

    [RelayCommand]
    private void NextFile()
    {
        CurrentBgymlFile = CurrentBgymlType?.GetFileBefore(CurrentBgymlFile);
    }

    partial void OnCurrentSelectionChanged(BgymlEntity? value)
    {
        if ((value as BgymlType ?? value?.Parent as BgymlType) is { } type && CurrentBgymlType != type) {
            CurrentBgymlType = type;
            CurrentBgymlFile = type.GetFileAfter(null);
        }
    }

    partial void OnCurrentBgymlFileChanged(BgymlFile? value)
    {
        if (value is null) {
            return;
        }

        ref var yaml = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _loadedBgymlFiles, value.CanonicalFilePath, out bool _);
        CurrentBgyml = yaml ??= value.GetYaml();
    }
}