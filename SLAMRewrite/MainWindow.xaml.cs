using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using SLAMRewrite.DataObjects;

namespace SLAMRewrite;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Dictionary<string, InMemoryAudioFile> _audioTracksDictionary = [];
    private readonly MediaPlayer _mediaPlayer = new();

    private Uri? _selectedUri = null;
    private SongStatus _songStatus = SongStatus.Stopped;

    public MainWindow()
    {
        InitializeComponent();
        _mediaPlayer.MediaEnded += (_, _) => { _songStatus = SongStatus.Stopped; };
    }

    private void ImportButton_OnClick(object sender, RoutedEventArgs e) =>
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

    private void PlayButton_OnClick(object sender, RoutedEventArgs e) =>
        HandleExceptionsWithMessageBox(() =>
        {
            switch (_songStatus)
            {
                // load song then play
                case SongStatus.Stopped:
                    if (_selectedUri is not null)
                    {
                        _mediaPlayer.Open(_selectedUri);
                        _mediaPlayer.Play();
                        _songStatus = SongStatus.Playing;
                    }

                    break;

                // play song
                case SongStatus.Paused:
                    _mediaPlayer.Play();
                    _songStatus = SongStatus.Playing;
                    break;

                // ignore input ?
                case SongStatus.Playing:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(_songStatus),
                        _songStatus,
                        "Check cases on _songStatus");
            }
        });

    private void PauseButton_OnClick(object sender, RoutedEventArgs e) =>
        HandleExceptionsWithMessageBox(() =>
        {
            switch (_songStatus)
            {
                // ignore input
                case SongStatus.Stopped:
                    break;

                // ignore input
                case SongStatus.Paused:
                    break;

                // play paused song
                case SongStatus.Playing:
                    _mediaPlayer.Pause();
                    _songStatus = SongStatus.Paused;
                    break;

                // ???
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(_songStatus),
                        _songStatus,
                        "Check cases on _songStatus");
            }
        });

    private void AudioTracks_OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
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

    private void SkipToNextButton_OnClick(object sender, RoutedEventArgs e)
    {
        HandleExceptionsWithMessageBox(() =>
        {
            _mediaPlayer.Stop();
            _songStatus = SongStatus.Stopped;
        });
    }
}