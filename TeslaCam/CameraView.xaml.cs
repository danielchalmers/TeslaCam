using System.Windows;
using System.Windows.Controls;
using Serilog;

namespace TeslaCam;

public partial class CameraView : UserControl
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(string),
        typeof(CameraView),
        new PropertyMetadata(null, OnSourceChanged));

    public static readonly DependencyProperty SpeedRatioProperty = DependencyProperty.Register(
        nameof(SpeedRatio),
        typeof(double),
        typeof(CameraView),
        new PropertyMetadata(1.0, OnSpeedRatioChanged));

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public double SpeedRatio
    {
        get => (double)GetValue(SpeedRatioProperty);
        set => SetValue(SpeedRatioProperty, value);
    }

    public CameraView()
    {
        InitializeComponent();
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CameraView)d;

        var uri = new Uri((string)e.NewValue);
        try
        {
            control.MediaElement.Open(uri);
        }
        catch (Exception ex)
        {
            Log.Error($"Invalid Source URI: {uri}, Exception: {ex.Message}");
        }
    }

    private static void OnSpeedRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CameraView)d;

        control.MediaElement.SpeedRatio = (double)e.NewValue;
    }
}
