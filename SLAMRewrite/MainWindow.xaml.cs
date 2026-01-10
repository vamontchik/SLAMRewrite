using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SLAMRewrite.DataObjects;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SLAMRewrite;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private string? _selectedTrack;
    private readonly int _outputToGameMicDeviceNumber;
    private readonly int _outputToDefaultDeviceNumber;
    private AudioFileReader? _audioFileReaderForGameMic;
    private AudioFileReader? _audioFileReaderForDefault;
    private WaveOutEvent? _outputToGameMicDevice;
    private WaveOutEvent? _outputToDefaultDevice;

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
        _outputToGameMicDeviceNumber = FindGameMicDeviceNumber();
        _outputToDefaultDeviceNumber = FindDefaultDeviceNumber();
    }

    private static int FindGameMicDeviceNumber()
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

    private static int FindDefaultDeviceNumber()
    {
        return 0;
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

            var fullPath = openFileDialog.FileName;
            AudioTracksListView.Items.Add(fullPath);
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
            var selection = e.AddedItems[0] as string;
            _selectedTrack = selection;
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
            if (_audioFileReaderForGameMic is not null)
                return;

            _audioFileReaderForGameMic = new AudioFileReader(_selectedTrack);
            _audioFileReaderForDefault = new AudioFileReader(_selectedTrack);
        });
    }

    private void PlayAudioStream()
    {
        HandleExceptionsWithMessageBox(() =>
        {
            if (_audioFileReaderForGameMic is null)
                return;

            if (_outputToGameMicDevice is null)
            {
                _outputToGameMicDevice = new WaveOutEvent { DeviceNumber = _outputToGameMicDeviceNumber };
                _outputToGameMicDevice.Init(_audioFileReaderForGameMic);
                _outputToGameMicDevice.Volume = 0.05f;

                _outputToDefaultDevice = new WaveOutEvent { DeviceNumber = _outputToDefaultDeviceNumber };
                var volumeProvider = new VolumeSampleProvider(_audioFileReaderForDefault.ToSampleProvider())
                {
                    Volume = 0.25f
                };
                _outputToDefaultDevice.Init(volumeProvider);

                _outputToGameMicDevice.PlaybackStopped += (_, _) =>
                {
                    StopAudioStream();
                    SongStatus = SongStatus.Stopped;
                };
                _outputToDefaultDevice.PlaybackStopped += (_, _) =>
                {
                    StopAudioStream();
                    SongStatus = SongStatus.Stopped;
                };
            }

            _outputToGameMicDevice.Play();
            _outputToDefaultDevice!.Play();
        });
    }

    private void PauseAudioStream()
    {
        HandleExceptionsWithMessageBox(() =>
        {
            if (_outputToGameMicDevice is null)
                return;

            _outputToGameMicDevice.Pause();
            _outputToDefaultDevice!.Pause();
        });
    }

    private void StopAudioStream()
    {
        HandleExceptionsWithMessageBox(() =>
        {
            _outputToGameMicDevice?.Stop();
            _outputToGameMicDevice?.Dispose();

            _outputToDefaultDevice?.Stop();
            _outputToDefaultDevice?.Dispose();

            _audioFileReaderForGameMic?.Dispose();
            _audioFileReaderForDefault?.Dispose();

            _outputToGameMicDevice = null;
            _outputToDefaultDevice = null;

            _audioFileReaderForGameMic = null;
            _audioFileReaderForDefault = null;
        });
    }

    private void MenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        HandleExceptionsWithMessageBox(() =>
        {
            if (_selectedTrack is null)
                return;

            var fileNameOnly = System.IO.Path.GetFileName(_selectedTrack);
            Clipboard.SetText(fileNameOnly);
        });
    }
}