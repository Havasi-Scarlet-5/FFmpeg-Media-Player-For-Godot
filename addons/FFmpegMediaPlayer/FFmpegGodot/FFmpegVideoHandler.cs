using System;
using System.Threading.Tasks;
using FFmpeg.AutoGen.Abstractions;
using Godot;

namespace FFmpegMediaPlayer.FFmpegGodot;

public sealed unsafe partial class FFmpegVideoHandler : RefCounted
{
    private readonly FFmpegVideoDecoder _decoder;

    private TextureRect _texture;

    private bool IsTextureValid => IsInstanceValid(_texture) && IsInstanceValid(_texture.Texture) && _texture.Texture is ImageTexture;

    private FFmpegYUVTextureHandler _textureHandler;

    public bool IsRunning { get; private set; }

    public bool IsFinished { get; private set; }

    private bool _canSkipFrames = true;

    public bool CanSkipFrames
    {
        get => GetCanSkipFrames();
        set => SetCanSkipFrames(value);
    }

    private double _currentFrameTime;

    public double Time
    {
        get => GetTime();
        set => SetTime(value);
    }

    private double _duration;

    public double Duration => GetDuration();

    private float _speed = 1.0f;

    public float Speed
    {
        get => GetSpeed();
        set => SetSpeed(value);
    }

    private Color _color = Colors.White;

    public Color Color
    {
        get => GetColor();
        set => SetColor(value);
    }

    private float _hue;

    public float Hue
    {
        get => GetHue();
        set => SetHue(value);
    }

    private float _saturation = 100.0f;

    public float Saturation
    {
        get => GetSaturation();
        set => SetSaturation(value);
    }

    private float _lightness = 50.0f;

    public float Lightness
    {
        get => GetLightness();
        set => SetLightness(value);
    }

    private float _contrast;

    public float Contrast
    {
        get => GetContrast();
        set => SetContrast(value);
    }

    private bool _chromakeyEnable;

    public bool ChromaKeyEnable
    {
        get => GetChromaEnable();
        set => SetChromaKeyEnable(value);
    }

    private Color _chromakeyColor = Colors.Green;

    public Color ChromaKeyColor
    {
        get => GetChromaKeyColor();
        set => SetChromaKeyColor(value);
    }

    private float _chromaKeyThreshold = 0.4f;

    public float ChromaKeyThreshold
    {
        get => GetChromaKeyThreshold();
        set => SetChromaKeyThreshold(value);
    }

    private float _chromaKeySmoothness = 0.1f;

    public float ChromaKeySmoothness
    {
        get => GetChromaKeySmoothness();
        set => SetChromaKeySmoothness(value);
    }

    private Task<bool> _seekTaskResult = Task.FromResult(true);

    public FFmpegVideoHandler(FFmpegVideoDecoder decoder, TextureRect texture)
    {
        _decoder = decoder;

        _duration = _decoder.DurationInSeconds;

        var image = Image.CreateEmpty(_decoder.Resolution.Width, _decoder.Resolution.Height, false, Image.Format.Rgb8);

        image.Fill(Colors.Black);

        texture.Texture = ImageTexture.CreateFromImage(image);

        texture.Show();

        _texture = texture;

        _textureHandler = new FFmpegYUVTextureHandler(_texture, _decoder);

        _decoder.SeekCompleted += OnSeekCompleted;

        while (_decoder.TryGetNextFrame(out var frame, out _))
        {
            UpdateVideoFrame(frame);
            break;
        }
    }

    private void OnSeekCompleted(AVFrame frame)
    {
        Callable.From(() => UpdateVideoFrame(frame)).CallDeferred();
    }

    public void Update(double clockTime)
    {
        if (!IsTextureValid || !IsRunning || !_seekTaskResult.IsCompletedSuccessfully)
            return;

        while (IsRunning && _currentFrameTime < clockTime && _decoder.TryGetNextFrame(out var frame, out var frameTime))
        {
            var skip = _canSkipFrames && Mathf.Abs(clockTime - frameTime) > 1.0 / _decoder.FrameRate;

            if (!skip)
                UpdateVideoFrame(frame);

            _currentFrameTime = frameTime;
        }

        IsFinished = clockTime >= Duration;

        if (IsFinished)
        {
            Stop();
            _currentFrameTime = _duration;
        }
    }

    public void Start()
    {
        if (IsRunning)
            return;

        IsFinished = false;

        IsRunning = true;
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
    }

    public bool GetCanSkipFrames()
    {
        return _canSkipFrames;
    }

    public void SetCanSkipFrames(bool canSkipFrames)
    {
        _canSkipFrames = canSkipFrames;
    }

    public double GetTime()
    {
        return Mathf.Clamp(_currentFrameTime, 0.0, _duration);
    }

    public void SetTime(double time, bool async = true)
    {
        IsFinished = false;

        time = Mathf.Clamp(time, 0.0, _duration);

        _currentFrameTime = time;

        if (async)
            _seekTaskResult = _decoder?.TrySeekAsync(_currentFrameTime);
        else
            _seekTaskResult = Task.FromResult(_decoder?.TrySeek(_currentFrameTime) ?? false);
    }

    public double GetDuration()
    {
        return _duration;
    }

    public float GetSpeed()
    {
        return _speed;
    }

    public void SetSpeed(float speed)
    {
        _speed = speed;
    }

    public Color GetColor()
    {
        return _color;
    }

    public void SetColor(Color color)
    {
        _color = color;
        _textureHandler?.SetTintColor(_color);
    }

    public float GetHue()
    {
        return _hue;
    }

    public void SetHue(float hue)
    {
        _hue = hue;
        _textureHandler?.SetHue(_hue);
    }

    public float GetSaturation()
    {
        return _saturation;
    }

    public void SetSaturation(float saturation)
    {
        _saturation = saturation;
        _textureHandler?.SetSaturation(_saturation);
    }

    public float GetLightness()
    {
        return _lightness;
    }

    public void SetLightness(float lightness)
    {
        _lightness = lightness;
        _textureHandler?.SetLightness(_lightness);
    }

    public float GetContrast()
    {
        return _contrast;
    }

    public void SetContrast(float contrast)
    {
        _contrast = contrast;
        _textureHandler?.SetContrast(_contrast);
    }

    public bool GetChromaEnable()
    {
        return _chromakeyEnable;
    }

    public void SetChromaKeyEnable(bool enable)
    {
        _chromakeyEnable = enable;
        _textureHandler?.SetChromaKeyEnable(enable);
    }

    public Color GetChromaKeyColor()
    {
        return _chromakeyColor;
    }

    public void SetChromaKeyColor(Color color)
    {
        _chromakeyColor = color;
        _textureHandler?.SetChromaKeyColor(color);
    }

    public float GetChromaKeyThreshold()
    {
        return _chromaKeyThreshold;
    }

    public void SetChromaKeyThreshold(float threshold)
    {
        _chromaKeyThreshold = threshold;
        _textureHandler?.SetChromaKeyThreshold(threshold);
    }

    public float GetChromaKeySmoothness()
    {
        return _chromaKeyThreshold;
    }

    public void SetChromaKeySmoothness(float smoothness)
    {
        _chromaKeySmoothness = smoothness;
        _textureHandler?.SetChromaKeySmoothness(smoothness);
    }

    private void UpdateVideoFrame(AVFrame frame)
    {
        try
        {
            var pixelFormat = (AVPixelFormat)frame.format;

            switch (pixelFormat)
            {
                case AVPixelFormat.AV_PIX_FMT_YUV420P:
                    {
                        var yHeight = frame.height;

                        var uvHeight = frame.height / 2;

                        var yStride = frame.linesize[0];

                        var y = new ReadOnlySpan<byte>(frame.data[0], yStride * yHeight);

                        var uStride = frame.linesize[1];

                        var u = new ReadOnlySpan<byte>(frame.data[1], uStride * uvHeight);

                        var vStride = frame.linesize[2];

                        var v = new ReadOnlySpan<byte>(frame.data[2], vStride * uvHeight);

                        var padding = yStride - frame.width;

                        _textureHandler?.UpdateYUVTexture(
                            yStride,
                            uStride,
                            vStride,
                            frame.height,
                            padding,
                            y, u, v
                        );

                        break;
                    }
                default:
                    FFmpegLogger.LogErr(this, "Unsupported pixel format: ", pixelFormat);
                    break;
            }
        }
        catch (Exception e)
        {
            FFmpegLogger.LogErr(this, "Error update video frame: ", e.Message);
        }
    }

    public Image GetCurrentFrameImage()
    {
        if (!_decoder.TryGetCurrentFrame(out var inFrame))
            return null;

        FFmpegVideoFrameConverter converter = null;

        try
        {
            var pixelFormat = (AVPixelFormat)inFrame.format;

            var width = _decoder.Resolution.Width;

            var height = _decoder.Resolution.Height;

            AVFrame outFrame;

            if (pixelFormat != AVPixelFormat.AV_PIX_FMT_RGB24)
            {
                converter = new FFmpegVideoFrameConverter(
                    new(inFrame.width, inFrame.height),
                    pixelFormat,
                    new(width, height),
                    AVPixelFormat.AV_PIX_FMT_RGB24
                );

                outFrame = converter.Convert(inFrame);
            }
            else
                outFrame = inFrame;

            return Image.CreateFromData(
                width,
                height,
                false,
                Image.Format.Rgb8,
                new ReadOnlySpan<byte>(outFrame.data[0], width * height * 3)
            );
        }
        catch (Exception e)
        {
            FFmpegLogger.LogErr(this, "Error getting current video frame: ", e.Message);
        }
        finally
        {
            converter?.Dispose();
        }

        return null;
    }

    public new void Dispose()
    {
        Stop();

        _textureHandler?.Dispose();

        _textureHandler = null;

        _texture?.Hide();

        _texture?.SetTexture(null);

        _texture?.SetMaterial(null);

        _texture = null;

        _decoder.SeekCompleted -= OnSeekCompleted;

        base.Dispose();
    }
}