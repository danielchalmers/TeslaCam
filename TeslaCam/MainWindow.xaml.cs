using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using TeslaCam.Data;

namespace TeslaCam;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow : Window
{
    [ObservableProperty]
    private CamClip _currentClip;

    [ObservableProperty]
    private string _errorMessage;

    public ObservableCollection<CamStorage> Storages { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        // Local directory.
        if (Directory.Exists("./TeslaCam"))
        {
            Storages.Add(new("./TeslaCam"));
        }

        // USB sticks.
        foreach (var storage in CamStorage.GetSticks())
        {
            Storages.Add(storage);
        }

        if (Storages.Count == 0)
        {
            Log.Debug("No storages found");
            ErrorMessage = "No TeslaCam folders found";
        }
        else
        {
            Log.Debug($"Found storages: {string.Join(", ", Storages)}");
            CurrentClip = Storages.First().Clips.FirstOrDefault();
        }
    }

    private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is CamClip selectedClip)
        {
            CurrentClip = selectedClip;
            Log.Debug($"Selected clip: {CurrentClip}");
        }
    }

    private void TreeView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TreeView treeView)
        {
            ExpandAllItems(treeView, treeView.Items);
        }
    }

    private static void ExpandAllItems(TreeView treeView, ItemCollection items)
    {
        Log.Debug("Expanding all tree view items");
        foreach (var item in items)
        {
            if (treeView.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeViewItem)
            {
                treeViewItem.IsExpanded = true;
                ExpandAllItems(treeView, treeViewItem.Items);
            }
        }
    }
}
