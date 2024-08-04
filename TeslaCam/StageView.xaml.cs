using System.Windows;
using System.Windows.Controls;
using TeslaCam.Data;

namespace TeslaCam;

public partial class StageView : UserControl
{
    private MediaElement _currentElement;
    private MediaElement _nextElement;

    public static readonly DependencyProperty CamClipProperty = DependencyProperty.Register(
        nameof(CamClip),
        typeof(CamClip),
        typeof(StageView),
        new PropertyMetadata(null, OnCamClipChanged));

    public static readonly DependencyProperty CameraNameProperty = DependencyProperty.Register(
        nameof(CameraName),
        typeof(string),
        typeof(StageView),
        new PropertyMetadata(null, OnCameraNameChanged));

    public CamClip CamClip
    {
        get => (CamClip)GetValue(CamClipProperty);
        set => SetValue(CamClipProperty, value);
    }

    public string CameraName
    {
        get => (string)GetValue(CameraNameProperty);
        set => SetValue(CameraNameProperty, value);
    }

    public StageView()
    {
        InitializeComponent();
        _currentElement = MediaElement1;
        _nextElement = MediaElement2;
    }

    private static void OnCamClipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (StageView)d;
        control.PlayCurrentChunk();
    }

    private static void OnCameraNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (StageView)d;
        control.PlayCurrentChunk();
    }

    private void PlayCurrentChunk()
    {
        if (CamClip?.CurrentChunk?.Value == null)
            return;

        var camFile = CamClip.CurrentChunk.Value.TryGetCamera(CameraName);
        if (camFile == null)
            return;

        _currentElement.Source = new Uri(camFile.FilePath);
        _currentElement.Play();
    }

    private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
    {
        _currentElement.Visibility = Visibility.Visible;
        _nextElement.Visibility = Visibility.Collapsed;
    }

    private void MediaElement1_MediaEnded(object sender, RoutedEventArgs e)
    {
        CamClip.NextChunk();
        _currentElement = MediaElement2;
        _nextElement = MediaElement1;
        PlayCurrentChunk();
    }

    private void MediaElement2_MediaEnded(object sender, RoutedEventArgs e)
    {
        CamClip.NextChunk();
        _currentElement = MediaElement1;
        _nextElement = MediaElement2;
        PlayCurrentChunk();
    }
}
