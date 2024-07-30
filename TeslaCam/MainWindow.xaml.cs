using System.Windows;
using TeslaCam.Data;

namespace TeslaCam;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var camStorage = CamStorage.GetSticks().FirstOrDefault();
        Console.WriteLine(camStorage.DirectoryPath);
    }
}