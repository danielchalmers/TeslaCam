﻿using System.Windows;
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

        MediaElement1.MediaOpened += MediaElement_MediaOpened;
        MediaElement2.MediaOpened += MediaElement_MediaOpened;
        MediaElement1.MediaEnded += MediaElement1_MediaEnded;
        MediaElement2.MediaEnded += MediaElement2_MediaEnded;
    }

    private static void OnCamClipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CameraView)d;
        control._currentChunk = (e.NewValue as CamClip).Chunks.First;
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
            return;

        var camFile = _currentChunk.Value.TryGetCamera(CameraName);
        if (camFile == null)
            return;

        _currentElement.Source = new Uri(camFile.FilePath);
        _currentElement.Play();
    }

    private void NextChunk()
    {
        _currentChunk = _currentChunk.Next;
        Log.Debug($"{CameraName}: Next chunk");
    }

    private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
    {
        _currentElement.Visibility = Visibility.Visible;
        _nextElement.Visibility = Visibility.Collapsed;

        FileStarted?.Invoke(this, EventArgs.Empty);
        Log.Debug($"{CameraName}: {_currentChunk.Value.Timestamp}");
    }

    private void MediaElement1_MediaEnded(object sender, RoutedEventArgs e)
    {
        if (_currentChunk.Next == null)
        {
            return;
        }

        NextChunk();
        _currentElement = MediaElement2;
        _nextElement = MediaElement1;
        PlayCurrentChunk();
    }

    private void MediaElement2_MediaEnded(object sender, RoutedEventArgs e)
    {
        if (_currentChunk.Next == null)
        {
            return;
        }

        NextChunk();
        _currentElement = MediaElement1;
        _nextElement = MediaElement2;
        PlayCurrentChunk();
    }

    private void UpdateLayoutBasedOnMini()
    {
        if (Mini)
        {
            Width = 256;
            Height = 192;
            NameTextBlock.Visibility = Visibility.Visible;
        }
        else
        {
            Width = double.NaN;
            Height = double.NaN;
            NameTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}
