using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace FFmpegMediaPlayer.FFmpegGodot;

[GlobalClass, Icon("res://addons/FFmpegMediaPlayer/FFmpegGodot/Icons/Player.svg")]
public partial class FFmpegPlayer : Control
{
    private FFmpegSource _source;

    [Export]
    public FFmpegSource Source
    {
        get => GetSource();
        set => SetSource(value);
    }

    [Export]
    public bool LoadAsync;

    [ExportCategory("Video")]

    [Export]
    public bool DisableVideo;

    private bool _canSkipFrames = true;

    [Export]
    public bool CanSkipFrames
    {
        get => _canSkipFrames;
        set
        {
            _canSkipFrames = value;
            VideoHandler?.SetCanSkipFrames(_canSkipFrames);
        }
    }

    [Export]
    public bool SeekAsync = true;

    public TextureRect TextureRect { get; private set; }

    private TextureRect.StretchModeEnum _stretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

    [Export]
    public TextureRect.StretchModeEnum StretchMode
    {
        get => _stretchMode;
        set
        {
            _stretchMode = value;

            if (TextureRect != null)
                TextureRect.StretchMode = _stretchMode;
        }
    }

    private Color _color = Colors.White;

    [Export]
    public Color Color
    {
        get => _color;
        set
        {
            _color = value;
            VideoHandler?.SetColor(_color);
        }
    }

    private float _hue;

    [Export(PropertyHint.Range, "0,360,1")]
    public float Hue
    {
        get => _hue;
        set
        {
            var v = Mathf.Clamp(Mathf.Snapped(value, 1.0f), 0.0f, 360.0f);

            _hue = v;

            VideoHandler?.SetHue(_hue);
        }
    }

    private float _saturation = 100.0f;

    [Export(PropertyHint.Range, "0,200,1")]
    public float Saturation
    {
        get => _saturation;
        set
        {
            var v = Mathf.Clamp(Mathf.Snapped(value, 1.0f), 0.0f, 200.0f);

            _saturation = v;

            VideoHandler?.SetSaturation(_saturation);
        }
    }

    private float _lightness = 50.0f;

    [Export(PropertyHint.Range, "0,100,1")]
    public float Lightness
    {
        get => _lightness;
        set
        {
            var v = Mathf.Clamp(Mathf.Snapped(value, 1.0f), 0.0f, 100.0f);

            _lightness = v;

            VideoHandler?.SetLightness(_lightness);
        }
    }

    private float _contrast;

    [Export(PropertyHint.Range, "-100,100,1")]
    public float Contrast
    {
        get => _contrast;
        set
        {
            var v = Mathf.Clamp(Mathf.Snapped(value, 1.0f), -100.0f, 100.0f);

            _contrast = v;

            VideoHandler?.SetContrast(_contrast);
        }
    }

    [ExportSubgroup("Chroma Key")]

    private bool _chromakeyEnable;

    [Export]
    public bool ChromaKeyEnable
    {
        get => _chromakeyEnable;
        set
        {
            _chromakeyEnable = value;
            VideoHandler?.SetChromaKeyEnable(_chromakeyEnable);
        }
    }

    private Color _chromaKeyColor = Colors.Green;

    [Export]
    public Color ChromaKeyColor
    {
        get => _chromaKeyColor;
        set
        {
            _chromaKeyColor = value;
            VideoHandler?.SetChromaKeyColor(_chromaKeyColor);
        }
    }

    private float _chromaKeyThreshold = 0.4f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float ChromaKeyThreshold
    {
        get => _chromaKeyThreshold;
        set
        {
            var v = Mathf.Clamp(Mathf.Snapped(value, 0.01f), 0.0f, 1.0f);

            _chromaKeyThreshold = v;

            VideoHandler?.SetChromaKeyThreshold(_chromaKeyThreshold);
        }
    }

    private float _chromaKeySmoothness = 0.1f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float ChromaKeySmoothness
    {
        get => _chromaKeySmoothness;
        set
        {
            var v = Mathf.Clamp(Mathf.Snapped(value, 0.01f), 0.0f, 1.0f);

            _chromaKeySmoothness = v;

            VideoHandler?.SetChromaKeySmoothness(_chromaKeySmoothness);
        }
    }

    [ExportCategory("Audio")]

    [Export]
    public bool DisableAudio;

    private float _bufferLength = 0.1f;

    [Export(PropertyHint.Range, "0.01,1,0.001")]
    public float BufferLength
    {
        get => _bufferLength;
        set
        {
            var v = Mathf.Clamp(Mathf.Snapped(value, 0.001f), 0.01f, 1.0f);
            _bufferLength = v;
        }
    }

    public AudioStreamPlayer AudioStreamPlayer { get; private set; }

    private float _pitch = 1.0f;

    [Export(PropertyHint.Range, "0.25,4,0.01")]
    public float Pitch
    {
        get => _pitch;
        set
        {
            var v = Mathf.Clamp(Mathf.Snapped(value, 0.01f), 0.25f, 4.0f);

            _pitch = v;

            AudioHandler?.SetPitch(_pitch);

            AudioHandler?.SetSpeed(_speed / _pitch);
        }
    }

    private float _volume = 1.0f;

    [Export(PropertyHint.Range, "0,1,0.001")]
    public float Volume
    {
        get => _volume;
        set
        {
            var v = Mathf.Clamp(Mathf.Snapped(value, 0.001f), 0.0f, 1.0f);

            _volume = v;

            AudioHandler?.SetVolume(_volume);
        }
    }

    private bool _mute;

    [Export]
    public bool Mute
    {
        get => _mute;
        set
        {
            _mute = value;
            AudioHandler?.SetMute(_mute);
        }
    }

    private string _bus = "Master";

    [Export]
    public string Bus
    {
        get => _bus;
        set
        {
            _bus = value;

            if (IsInstanceValid(AudioStreamPlayer))
                AudioStreamPlayer.Bus = _bus;
        }
    }

    [Export]
    public double VideoDelayCompensation = -0.1;

    [Export]
    public double VideoClockSyncThreshold = 0.05;

    [ExportCategory("Playback")]

    [Export]
    public bool AutoPlay;

    private float _speed = 1.0f;

    [Export(PropertyHint.Range, "0.25,4,0.01")]
    public float Speed
    {
        get => _speed;
        set
        {
            var v = Mathf.Clamp(Mathf.Snapped(value, 0.01f), 0.25f, 4.0f);

            _speed = v;

            VideoHandler?.SetSpeed(_speed);

            AudioHandler?.SetSpeed(_speed / _pitch);
        }
    }

    [Export]
    public bool Loop;

    [ExportCategory("Misc")]

    private bool _debugLog;

    [Export]
    public bool DebugLog
    {
        get => _debugLog;
        set
        {
            _debugLog = value;
            FFmpegStatic.DebugLog = _debugLog;
        }
    }

    public FFmpegVideoDecoder VideoDecoder { get; private set; }

    public FFmpegVideoHandler VideoHandler { get; private set; }

    public FFmpegAudioDecoder AudioDecoder { get; private set; }

    public FFmpegAudioHandler AudioHandler { get; private set; }

    public bool IsVideoValid => VideoDecoder != null && !VideoDecoder.IsThumbnail && VideoHandler != null;

    public bool IsAudioValid => AudioDecoder != null && AudioHandler != null;

    public bool IsLoaded { get; private set; }

    private double _clockTime;

    public double ClockTime => _clockTime;

    public double Time => IsVideoValid ? VideoHandler.Time : IsAudioValid ? AudioHandler.Time : 0.0;

    public double Duration => IsVideoValid ? VideoHandler.Duration : IsAudioValid ? AudioHandler.Duration : 0.0;

    public bool IsPlaying { get; private set; }

    public bool IsFinished { get; private set; }

    [Signal]
    public delegate void LoadedEventHandler();

    [Signal]
    public delegate void FinishedEventHandler();

    [Signal]
    public delegate void ClosedEventHandler();

    private double _lastAudioTime;

    private CancellationTokenSource _loadCTS;

    public override void _Ready()
    {
        FFmpegStatic.DebugLog = _debugLog;

        if (TextureRect == null)
        {
            TextureRect = new TextureRect
            {
                Texture = new ImageTexture(),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = _stretchMode
            };

            AddChild(TextureRect, true, InternalMode.Back);

            TextureRect.SetAnchorsPreset(LayoutPreset.FullRect, true);

            TextureRect.Hide();
        }

        if (AudioStreamPlayer == null)
        {
            AudioStreamPlayer = new AudioStreamPlayer
            {
                VolumeDb = Mathf.LinearToDb(Volume)
            };

            AddChild(AudioStreamPlayer, true, InternalMode.Back);

            AudioStreamPlayer.Bus = Bus;
        }

        if (_source != null)
            SetSource(_source);
    }

    public override void _Process(double delta)
    {
        if (IsPlaying)
        {
            _clockTime += delta * _speed;

            if (IsAudioValid)
            {
                var audioTime = AudioHandler.Time;

                var audioDelta = audioTime - _lastAudioTime;

                if (Mathf.Abs(audioDelta) > 0.0)
                {
                    var absDrift = Mathf.Abs(audioTime - _clockTime);

                    var outOfSync = absDrift > VideoClockSyncThreshold;

                    if (outOfSync)
                        _clockTime = audioTime;
                }

                _lastAudioTime = audioTime;
            }
        }

        VideoHandler?.Update(_clockTime + (IsAudioValid ? VideoDelayCompensation : 0.0));

        AudioHandler?.Update();

        var videoFinished = (!IsVideoValid || VideoHandler.IsFinished) && IsLoaded;

        var audioFinished = (!IsAudioValid || AudioHandler.IsFinished) && IsLoaded;

        IsFinished = videoFinished && audioFinished;

        if (IsFinished && IsPlaying)
        {
            if (Loop)
            {
                Stop();
                Play();
            }
            else
            {
                Pause();
                EmitSignal(SignalName.Finished);
            }
        }
    }

    public override void _ExitTree()
    {
        Close();
    }

    public FFmpegSource GetSource()
    {
        return _source;
    }

    private void LoadDecoders(FFmpegSource source)
    {
        var src = new FFmpegMediaSource() { Url = source.Url };

        if (src.Url.StartsWith("res://"))
            src.Buffer = FileAccess.GetFileAsBytes(src.Url);

        if (!DisableVideo)
            VideoDecoder = new FFmpegVideoDecoder(src);

        if (!DisableAudio)
            AudioDecoder = new FFmpegAudioDecoder(src);
    }

    public async void SetSource(FFmpegSource mediaSource)
    {
        Close();

        _source = mediaSource;

        if (!IsNodeReady() || _source == null)
            return;

        if (LoadAsync)
        {
            _loadCTS = new();

            try
            {
                await Task.Run(() => LoadDecoders(_source), _loadCTS.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                _loadCTS?.Dispose();
                _loadCTS = null;
            }
        }
        else
            LoadDecoders(_source);

        if (VideoDecoder?.Exist ?? false)
        {
            VideoHandler = new FFmpegVideoHandler(VideoDecoder, TextureRect);

            VideoHandler?.SetCanSkipFrames(_canSkipFrames);

            VideoHandler?.SetSpeed(_speed);

            VideoHandler?.SetColor(_color);

            VideoHandler?.SetHue(_hue);

            VideoHandler?.SetSaturation(_saturation);

            VideoHandler?.SetLightness(_lightness);

            VideoHandler?.SetContrast(_contrast);

            VideoHandler?.SetChromaKeyEnable(_chromakeyEnable);

            VideoHandler?.SetChromaKeyColor(_chromaKeyColor);

            VideoHandler?.SetChromaKeyThreshold(_chromaKeyThreshold);

            VideoHandler?.SetChromaKeySmoothness(_chromaKeySmoothness);
        }
        else
            CloseVideo();

        if (AudioDecoder?.Exist ?? false)
        {
            AudioHandler = new FFmpegAudioHandler(AudioDecoder, AudioStreamPlayer, _bufferLength);

            AudioHandler?.SetPitch(_pitch);

            AudioHandler?.SetSpeed(_speed / _pitch);

            AudioHandler?.SetVolume(_volume);

            AudioHandler?.SetMute(Mute);
        }
        else
            CloseAudio();

        if (IsVideoValid || IsAudioValid)
        {
            IsLoaded = true;

            EmitSignal(SignalName.Loaded);

            if (AutoPlay)
                Play();
        }
    }

    public void Open(FFmpegSource source)
    {
        SetSource(source);
    }

    public void Close()
    {
        _loadCTS?.Cancel();

        _loadCTS?.Dispose();

        _loadCTS = null;

        Stop();

        CloseVideo();

        CloseAudio();

        _source = null;

        IsLoaded = false;

        EmitSignal(SignalName.Closed);
    }

    private void CloseVideo()
    {
        VideoHandler?.Dispose();
        VideoHandler = null;

        VideoDecoder?.Dispose();
        VideoDecoder = null;
    }

    private void CloseAudio()
    {
        AudioHandler?.Dispose();
        AudioHandler = null;

        AudioDecoder?.Dispose();
        AudioDecoder = null;
    }

    public void Play()
    {
        if (!IsLoaded || IsPlaying)
            return;

        IsFinished = false;

        IsPlaying = true;

        VideoHandler?.Start();

        AudioHandler?.Start();
    }

    public void Pause()
    {
        if (!IsPlaying)
            return;

        IsPlaying = false;

        VideoHandler?.Stop();

        AudioHandler?.Stop();
    }

    public void Stop()
    {
        Pause();
        Seek(0.0);
    }

    public void Seek(double time)
    {
        IsFinished = false;

        time = Mathf.Clamp(time, 0.0, Duration);

        var seekToTheEnd = time >= Duration;

        VideoHandler?.SetTime(seekToTheEnd ? VideoHandler?.Duration ?? 0.0 : time, SeekAsync);

        AudioHandler?.SetTime(seekToTheEnd ? AudioHandler?.Duration ?? 0.0 : time);

        _clockTime = _lastAudioTime = seekToTheEnd ? Mathf.Max(
            VideoHandler?.Duration ?? 0.0,
            AudioHandler?.Duration ?? 0.0
        ) : time;
    }
}