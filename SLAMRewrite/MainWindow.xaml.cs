using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NAudio.Wave;

namespace SLAMRewrite;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly SimpleAudioDevice _outputToGame;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SongStatus SongStatus
    {
        get
        {
            if (_outputToGame.IsPaused)
                return SongStatus.Paused;

            if (_outputToGame.IsPlaying)
                return SongStatus.Playing;

            return SongStatus.Stopped;
        }
    }

    public string? NowPlaying => System.IO.Path.GetFileNameWithoutExtension(_outputToGame.GetFullPath());

    private string? SelectedTrack { get; set; } = null;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        _outputToGame = new SimpleAudioDeviceWithVolumeProvider(FindGameDeviceNumber(), defaultVolume: 0.1f);
    }

    private static int FindGameDeviceNumber()
    {
        const string deviceNameSearchString = "CABLE Input";

        for (var i = 0; i < WaveOut.DeviceCount; i++)
        {
            var capabilities = WaveOut.GetCapabilities(i);
            if (capabilities.ProductName.Contains(deviceNameSearchString))
            {
                return i;
            }
        }

        return -1;
    }

    private void AudioTracksListView_OnSelectionChanged(object _, SelectionChangedEventArgs e) =>
        HandleExceptionsWithMessageBox(() =>
        {
            if (e.AddedItems.Count != 1)
                return;

            var selection = e.AddedItems[0] as string;
            SelectedTrack = selection;
        });

    private void AddSong_OnItemClick(object _, RoutedEventArgs _2) =>
        HandleExceptionsWithMessageBox(() =>
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Music Files|*.mp3;*.wav;*.opus;*.ogg|All Files|*.*"
            };

            var hitOk = openFileDialog.ShowDialog();

            if (hitOk is null || !hitOk.Value)
                return;

            var fullPath = openFileDialog.FileName;
            AudioTracksListView.Items.Add(fullPath);
        });

    private void DeleteSong_OnItemClick(object _, RoutedEventArgs _2) =>
        HandleExceptionsWithMessageBox(() =>
        {
            if (SelectedTrack is null)
            {
                var __ = MessageBox.Show(
                    messageBoxText: "No selected track to delete.",
                    caption: "Error",
                    button: MessageBoxButton.OK,
                    icon: MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(
                messageBoxText: $"Are you sure you want to delete \'{SelectedTrack}\'?",
                caption: "Confirmation",
                button: MessageBoxButton.YesNo,
                icon: MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
                return;

            AudioTracksListView.Items.Remove(SelectedTrack);
            SelectedTrack = null;
        });

    private void PlayButton_OnClick(object _, RoutedEventArgs _2) =>
        HandleExceptionsWithMessageBoxWithFinally(
            tryAction: () =>
            {
                if (!_outputToGame.IsPaused && !SelectedCurrentlyPlayingTrack())
                {
                    _outputToGame.PlaybackStopped -= OnPlaybackStopped;
                    _outputToGame.Close();
                    _outputToGame.Open(SelectedTrack);
                    _outputToGame.PlaybackStopped += OnPlaybackStopped;
                }

                _outputToGame.Play();
            },
            finallyAction: FireStatusPropertiesChanged);

    private void PauseButton_OnClick(object _, RoutedEventArgs _2) =>
        HandleExceptionsWithMessageBoxWithFinally(
            tryAction: () => _outputToGame.Pause(),
            finallyAction: FireStatusPropertiesChanged);

    private void StopFlushButton_OnClick(object _, RoutedEventArgs _2) =>
        HandleExceptionsWithMessageBoxWithFinally(
            tryAction: () =>
            {
                _outputToGame.PlaybackStopped -= OnPlaybackStopped;
                _outputToGame.Close();
            },
            finallyAction: FireStatusPropertiesChanged);

    private static void HandleExceptionsWithMessageBox(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                messageBoxText: ex.Message,
                caption: "Something went wrong...",
                button: MessageBoxButton.OK,
                icon: MessageBoxImage.Error);
        }
    }

    private static void HandleExceptionsWithMessageBoxWithFinally(Action tryAction, Action finallyAction)
    {
        try
        {
            tryAction();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                messageBoxText: ex.Message,
                caption: "Something went wrong...",
                button: MessageBoxButton.OK,
                icon: MessageBoxImage.Error);
        }
        finally
        {
            finallyAction();
        }
    }

    private void FireStatusPropertiesChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SongStatus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NowPlaying)));
    }

    private void OnPlaybackStopped(object? o, EventArgs eventArgs) => FireStatusPropertiesChanged();

    private bool SelectedCurrentlyPlayingTrack() => _outputToGame.GetFullPath()?.Equals(SelectedTrack) ?? false;
}