using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SLAMRewrite.DataObjects;
using NAudio.Wave;

namespace SLAMRewrite;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly Dictionary<string, InMemoryAudioFile> _audioTracksDictionary = [];
    private string? _selectedTrack;
    private readonly int _deviceNumber;
    private AudioFileReader? _audioFileReader;
    private WaveOutEvent? _outputDevice;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SongStatus SongStatus
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SongStatus)));
        }
    } = SongStatus.Stopped;

    public MainWindow()
    {
        DataContext = this;
        InitializeComponent();
        _deviceNumber = FindDeviceNumber();
    }

    private static int FindDeviceNumber()
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

    private void ImportButton_OnClick(object _, RoutedEventArgs _2) =>
        HandleExceptionsWithMessageBox(() =>
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Music Files|*.mp3;*.wav;*.opus;*.ogg|All Files|*.*"
            };

            var hitOk = openFileDialog.ShowDialog();

            if (hitOk is null || !hitOk.Value)
                return;

            var fullFilePath = openFileDialog.FileName;
            var onlyFileName = openFileDialog.SafeFileName;

            _audioTracksDictionary[onlyFileName] = new InMemoryAudioFile(
                FileName: onlyFileName,
                FullFilePath: fullFilePath);
            AudioTracksListView.Items.Add(onlyFileName);
        });

    private void PlayButton_OnClick(object _, RoutedEventArgs _2) =>
        HandleExceptionsWithMessageBox(() =>
        {
            switch (SongStatus)
            {
                // load song then play
                case SongStatus.Stopped:
                    if (_selectedTrack is not null)
                    {
                        OpenAudioStream();
                        PlayAudioStream();
                        SongStatus = SongStatus.Playing;
                    }

                    break;

                // play paused song
                case SongStatus.Paused:
                    PlayAudioStream();
                    SongStatus = SongStatus.Playing;
                    break;

                // ignore input if already playing
                case SongStatus.Playing:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(SongStatus),
                        SongStatus,
                        "Check cases on SongStatus");
            }
        });

    private void PauseButton_OnClick(object _, RoutedEventArgs _2) =>
        HandleExceptionsWithMessageBox(() =>
        {
            if (SongStatus is not SongStatus.Playing)
                return;
            PauseAudioStream();
            SongStatus = SongStatus.Paused;
        });

    private void AudioTracks_OnSelectionChanged(object _, SelectionChangedEventArgs e) =>
        HandleExceptionsWithMessageBox(() =>
        {
            // TODO: working with only first selection here
            var firstSelection = e.AddedItems[0] as string;

            // TODO: is checking for firstSelection being null necessary
            _selectedTrack = _audioTracksDictionary[firstSelection!].FullFilePath;
        });

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

    private void SkipToNextButton_OnClick(object _, RoutedEventArgs _2)
    {
        HandleExceptionsWithMessageBox(() =>
        {
            StopAudioStream();
            SongStatus = SongStatus.Stopped;
        });
    }

    private void OpenAudioStream()
    {
        HandleExceptionsWithMessageBox(() =>
        {
            if (_audioFileReader is not null)
                return;

            _audioFileReader = new AudioFileReader(_selectedTrack);
        });
    }

    private void PlayAudioStream()
    {
        HandleExceptionsWithMessageBox(() =>
        {
            if (_audioFileReader is null)
                return;

            if (_outputDevice is null)
            {
                _outputDevice = new WaveOutEvent { DeviceNumber = _deviceNumber };
                _outputDevice.Init(_audioFileReader);
                _outputDevice.PlaybackStopped += (_, _) => SongStatus = SongStatus.Stopped;
            }

            _outputDevice.Play();
        });
    }

    private void PauseAudioStream()
    {
        HandleExceptionsWithMessageBox(() =>
        {
            if (_outputDevice is null)
                return;

            _outputDevice.Pause();
        });
    }

    private void StopAudioStream()
    {
        HandleExceptionsWithMessageBox(() =>
        {
            _outputDevice?.Stop();
            _outputDevice?.Dispose();
            _audioFileReader?.Dispose();

            _outputDevice = null;
            _audioFileReader = null;
        });
    }
}