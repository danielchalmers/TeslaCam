using System.Windows;
using System.Windows.Controls;
using Serilog;
using TeslaCam.Data;

namespace TeslaCam;

public partial class CameraView : UserControl
{
    private LinkedListNode<CamClipChunk> _currentChunk;
    private MediaElement _currentElement;
    private MediaElement _nextElement;

    public static readonly DependencyProperty CamClipProperty = DependencyProperty.Register(
        nameof(CamClip),
        typeof(CamClip),
        typeof(CameraView),
        new PropertyMetadata(null, OnCamClipChanged));

    public static readonly DependencyProperty CameraPathProperty = DependencyProperty.Register(
        nameof(CameraPath),
        typeof(string),
        typeof(CameraView));

    public static readonly DependencyProperty CameraNameProperty = DependencyProperty.Register(
        nameof(CameraName),
        typeof(string),
        typeof(CameraView));

    public static readonly DependencyProperty MiniProperty = DependencyProperty.Register(
        nameof(Mini),
        typeof(bool),
        typeof(CameraView),
        new PropertyMetadata(false, OnMiniChanged));

    public CamClip CamClip
    {
        get => (CamClip)GetValue(CamClipProperty);
        set => SetValue(CamClipProperty, value);
    }

    public string CameraPath
    {
        get => (string)GetValue(CameraPathProperty);
        set => SetValue(CameraPathProperty, value);
    }

    public string CameraName
    {
        get => (string)GetValue(CameraNameProperty);
        set => SetValue(CameraNameProperty, value);
    }

    public bool Mini
    {
        get => (bool)GetValue(MiniProperty);
        set => SetValue(MiniProperty, value);
    }

    public event EventHandler FileStarted;

    public CameraView()
    {
        InitializeComponent();
        _currentElement = MediaElement1;
        _nextElement = MediaElement2;
    }

    private static void OnCamClipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CameraView)d;

        control._currentChunk = (e.NewValue as CamClip)?.Chunks?.First;
        control.PlayCurrentChunk();
    }

    private static void OnMiniChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CameraView)d;
        control.UpdateLayoutBasedOnMini();
    }

    private void PlayCurrentChunk()
    {
        if (_currentChunk?.Value == null)
        {
            _currentElement.Stop();
            _currentElement.Visibility = Visibility.Collapsed;
            _nextElement.Stop();
            _nextElement.Visibility = Visibility.Collapsed;
            return;
        }

        var camFile = _currentChunk.Value.Files.GetValueOrDefault(CameraPath);
        if (camFile == null)
            return;

        _currentElement.Source = new Uri(camFile.FilePath);
        _currentElement.Play();
    }

    private void NextChunk()
    {
        _currentChunk = _currentChunk.Next;
    }

    private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
    {
        _currentElement.Visibility = Visibility.Visible;
        _nextElement.Visibility = Visibility.Collapsed;

        Log.Debug($"{CameraPath}: {_currentChunk?.Value?.Timestamp}");
        FileStarted?.Invoke(this, EventArgs.Empty);
    }

    private void MediaElement1_MediaEnded(object sender, RoutedEventArgs e)
    {
        if (_currentChunk?.Next == null)
        {
            Log.Debug($"{CameraPath}: view 1 ended with no chunks left");
            return;
        }

        Log.Debug($"{CameraPath}: view 1 ended; playing next chunk");

        NextChunk();
        _currentElement = MediaElement2;
        _nextElement = MediaElement1;
        PlayCurrentChunk();
    }

    private void MediaElement1_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        Log.Debug($"{CameraPath}: view 1 failed");
    }

    private void MediaElement2_MediaEnded(object sender, RoutedEventArgs e)
    {
        if (_currentChunk?.Next == null)
        {
            Log.Debug($"{CameraPath}: view 2 ended with no chunks left");
            return;
        }

        Log.Debug($"{CameraPath}: view 2 ended; playing next chunk");

        NextChunk();
        _currentElement = MediaElement1;
        _nextElement = MediaElement2;
        PlayCurrentChunk();
    }

    private void MediaElement2_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        Log.Debug($"{CameraPath}: view 2 failed");
    }

    private void UpdateLayoutBasedOnMini()
    {
        if (Mini)
        {
            Height = 192; // 1/5 of a full cam.
            NameTextBlock.Visibility = Visibility.Visible;
        }
        else
        {
            Height = double.NaN;
            NameTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}
