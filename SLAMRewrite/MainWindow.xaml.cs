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
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SongStatus)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NowPlaying)));
        }
    } = SongStatus.Stopped;

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
                return;

            var result = MessageBox.Show(
                messageBoxText: $"Are you sure you want to delete {SelectedTrack}?",
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
                if (SongStatus is not SongStatus.Paused)
                    OpenAudioStream();
                PlayAudioStream();
                SongStatus = SongStatus.Playing;
            },
            finallyAction: () =>
            {
                if (!_outputToGame.IsPlaying)
                    SongStatus = SongStatus.Stopped;
            });

    private void PauseButton_OnClick(object _, RoutedEventArgs _2) =>
        HandleExceptionsWithMessageBox(() =>
        {
            if (SongStatus is not SongStatus.Playing)
                return;
            PauseAudioStream();
            SongStatus = SongStatus.Paused;
        });

    private void StopFlushButton_OnClick(object _, RoutedEventArgs _2) =>
        HandleExceptionsWithMessageBox(() =>
        {
            if (SongStatus is SongStatus.Stopped)
                return;
            StopAudioStream();
            SongStatus = SongStatus.Stopped;
        });

    private void OpenAudioStream()
    {
        if (_outputToGame.IsOpened)
            _outputToGame.Close();

        _outputToGame.Open(SelectedTrack);
    }

    private void PlayAudioStream()
    {
        _outputToGame.Play();
        _outputToGame.PlaybackStopped += OnPlaybackStopped;
    }

    private void PauseAudioStream()
    {
        _outputToGame.Pause();
    }

    private void StopAudioStream()
    {
        _outputToGame.PlaybackStopped -= OnPlaybackStopped;
        _outputToGame.Close();
    }

    private void OnPlaybackStopped(object? _, EventArgs _2)
    {
        SongStatus = SongStatus.Stopped;
    }

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
}