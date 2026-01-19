using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SLAMRewrite;

public abstract class SimpleAudioDevice
{
    protected AudioFileReader? AudioFileReader;
    private WaveOutEvent? _waveOutEvent;

    public bool IsOpened => AudioFileReader is not null;

    public bool IsPlaying => _waveOutEvent is not null && !IsPaused;

    // Should be 'false' when no audio is playing. Default value of 'false'.
    public bool IsPaused { get; private set; } = false;

    public event EventHandler PlaybackStopped = delegate { };

    public void Open(string? fullFilePath)
    {
        if (string.IsNullOrEmpty(fullFilePath))
            throw new Exception($"Invalid file path. Did you select a track?");
        AudioFileReader = new AudioFileReader(fullFilePath);
    }

    public void Play()
    {
        // aka !IsOpened, but null check is better so compiler sees AudioFileReader as non-null
        if (AudioFileReader is null)
            return;

        _waveOutEvent ??= CreateWaveEvent();

        _waveOutEvent.PlaybackStopped += OnPlaybackStopped;

        _waveOutEvent.Play();

        IsPaused = false;
    }

    protected abstract WaveOutEvent CreateWaveEvent();

    public void Pause()
    {
        _waveOutEvent?.Pause();

        IsPaused = true;
    }

    public void Close()
    {
        _waveOutEvent?.PlaybackStopped -= OnPlaybackStopped;

        _waveOutEvent?.Dispose();
        _waveOutEvent = null;

        AudioFileReader?.Dispose();
        AudioFileReader = null;

        IsPaused = false; // reset to initial value
    }

    public string? GetFullPath() => AudioFileReader?.FileName;

    private void OnPlaybackStopped(object? sender, EventArgs e)
    {
        Close();
        PlaybackStopped(this, EventArgs.Empty);
    }
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