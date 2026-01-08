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

    public MainWindow()
    {
        InitializeComponent();
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
        HandleExceptionsWithMessageBox(() => _mediaPlayer.Play());

    private void AudioTracks_OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        HandleExceptionsWithMessageBox(() =>
        {
            // TODO: working with only first selection here...
            if (e.AddedItems[0] is not string fileNameOfSelection)
            {
                throw new Exception("Selected file with `null` as the file name");
            }

            var selectedTrack = _audioTracksDictionary[fileNameOfSelection];
            _mediaPlayer.Open(new Uri(selectedTrack.FullFilePath));
        });

    private void PauseButton_OnClick(object sender, RoutedEventArgs e) =>
        HandleExceptionsWithMessageBox(() => _mediaPlayer.Pause());

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
}