using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SLAMRewrite;

public abstract class SimpleAudioDevice
{
    protected AudioFileReader? AudioFileReader;
    private WaveOutEvent? _waveOutEvent;

    public bool IsOpened => AudioFileReader is not null;

    public bool IsPlaying => IsOpened &&
                             _waveOutEvent?.PlaybackState is PlaybackState.Playing;
    
    public bool IsPaused => IsOpened &&
                            _waveOutEvent?.PlaybackState is PlaybackState.Paused;

    public void Open(string? fullFilePath)
    {
        if (string.IsNullOrEmpty(fullFilePath))
            throw new Exception("Invalid file path. Did you select a track?");
        AudioFileReader = new AudioFileReader(fullFilePath);
    }

    public void Play()
    {
        // Open() must be called first
        if (AudioFileReader is null)
            return;

        if (_waveOutEvent is null)
        {
            _waveOutEvent ??= CreateWaveEvent();
            _waveOutEvent.PlaybackStopped += OnPlaybackStopped;
        }

        _waveOutEvent.Play();
    }

    protected abstract WaveOutEvent CreateWaveEvent();

    public void Pause()
    {
        if (_waveOutEvent is null)
            return;

        if (_waveOutEvent.PlaybackState is PlaybackState.Paused)
            return;

        _waveOutEvent.Pause();
    }

    public void Close()
    {
        _waveOutEvent?.PlaybackStopped -= OnPlaybackStopped;

        _waveOutEvent?.Dispose();
        _waveOutEvent = null;

        AudioFileReader?.Dispose();
        AudioFileReader = null;
    }

    public string? GetFullPath() => AudioFileReader?.FileName;

    private void OnPlaybackStopped(object? sender, EventArgs e)
    {
        Close();
        PlaybackStopped(sender, e);
    }

    public event EventHandler PlaybackStopped = delegate { };
}

public class SimpleAudioDeviceWithVolumeProvider(int deviceNumber, float defaultVolume) : SimpleAudioDevice
{
    protected override WaveOutEvent CreateWaveEvent()
    {
        var waveOutEvent = new WaveOutEvent { DeviceNumber = deviceNumber };
        var volumeProvider = new VolumeSampleProvider(AudioFileReader.ToSampleProvider())
        {
            Volume = defaultVolume
        };
        waveOutEvent.Init(volumeProvider);
        return waveOutEvent;
    }
}