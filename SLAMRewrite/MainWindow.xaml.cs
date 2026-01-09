using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using SLAMRewrite.DataObjects;

namespace SLAMRewrite;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly Dictionary<string, InMemoryAudioFile> _audioTracksDictionary = [];
    private readonly MediaPlayer _mediaPlayer = new();

    private Uri? _selectedUri = null;

    public event PropertyChangedEventHandler? PropertyChanged = null;

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
        _mediaPlayer.MediaEnded += (_, _) => SongStatus = SongStatus.Stopped;
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
            // TODO: async version with proper exception handling
            var musicFileContents = File.ReadAllBytes(fullFilePath);

            _audioTracksDictionary[onlyFileName] = new InMemoryAudioFile(
                FileName: onlyFileName,
                FullFilePath: fullFilePath,
                Bytes: musicFileContents);
            AudioTracksListView.Items.Add(onlyFileName);
        });

    private void PlayButton_OnClick(object _, RoutedEventArgs _2) =>
        HandleExceptionsWithMessageBox(() =>
        {
            switch (SongStatus)
            {
                // load song then play
                case SongStatus.Stopped:
                    if (_selectedUri is not null)
                    {
                        _mediaPlayer.Open(_selectedUri);
                        _mediaPlayer.Play();
                        SongStatus = SongStatus.Playing;
                    }

                    break;

                // play paused song
                case SongStatus.Paused:
                    _mediaPlayer.Play();
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
            _mediaPlayer.Pause();
            SongStatus = SongStatus.Paused;
        });

    private void AudioTracks_OnSelectionChanged(object _, SelectionChangedEventArgs e) =>
        HandleExceptionsWithMessageBox(() =>
        {
            // TODO: working with only first selection here
            var firstSelection = e.AddedItems[0] as string;

            // TODO: is checking for firstSelection being null necessary
            var fullFilePath = _audioTracksDictionary[firstSelection!].FullFilePath;

            _selectedUri = new Uri(fullFilePath);
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
            _mediaPlayer.Stop();
            SongStatus = SongStatus.Stopped;
        });
    }
}