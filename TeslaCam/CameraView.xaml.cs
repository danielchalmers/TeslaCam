using System.Windows;
using System.Windows.Controls;

namespace TeslaCam;

/// <summary>
/// Interaction logic for CameraView.xaml
/// </summary>
public partial class CameraView : UserControl
{
    public CameraView()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty SourceUriProperty =
        DependencyProperty.Register(nameof(SourceUri), typeof(Uri), typeof(CameraView));

    public Uri SourceUri
    {
        get => (Uri)GetValue(SourceUriProperty);
        set => SetValue(SourceUriProperty, value);
    }

    public static readonly DependencyProperty FeedNameProperty =
        DependencyProperty.Register(nameof(FeedName), typeof(string), typeof(CameraView));

    public string FeedName
    {
        get => (string)GetValue(FeedNameProperty);
        set => SetValue(FeedNameProperty, value);
    }
}
