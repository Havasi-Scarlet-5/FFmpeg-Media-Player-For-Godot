using System;
using Godot;
using FFmpegMediaPlayer.FFmpegGodot;

public partial class FFmpegMediaPlayerTest : Control
{
    [Export]
    private FFmpegPlayer _ffmpegPlayer;

    [Export]
    private Label _debugLabel;

    [Export]
    private Label _timeLabel;

    [Export]
    private Label _durationLabel;

    [Export]
    private HSlider _timeSlider;

    [Export]
    private Button _playbackButton;

    [Export]
    private Texture2D _playIcon;

    [Export]
    private Texture2D _pauseIcon;

    [Export]
    private Button _stopButton;

    [Export]
    private Button _loopButton;

    [Export]
    private Button _controllerButton;

    [Export]
    private Button _fullScreenButton;

    [Export]
    private Texture2D _fullScreenOnIcon;

    [Export]
    private Texture2D _fullScreenOffIcon;

    [Export]
    private FileDialog _openFileDialog;

    [Export]
    private SettingsWindow _settingsWindow;

    [Export]
    private VBoxContainer _ui;

    [Export]
    private MenuButton _fileMenuButton;

    private double _showTimeout;

    private bool _newMediaFileLoaded;

    private bool _isPlaying;

    private DisplayServer.WindowMode _lastWindowMode;

    private Vector2 _lastMousePos;

    public override void _Ready()
    {
        if (OS.GetName() == "Android")
            OS.RequestPermissions();

        _fullScreenButton.Pressed += ToggleFullScreen;

        // You can add more it you like
        _openFileDialog.AddFilter("*.mp4, *.webm, *.mpg, *.mpeg, *.mkv, *.avi, *.mov, *.wmv, *.ogv", "Supported Video Files");

        // You can add more it you like
        _openFileDialog.AddFilter("*.mp3, *.ogg, *.wav, *.flac", "Supported Audio Files");

        // You can load from (res://) by using this:
        // var source = GD.Load<FFmpegGodotMediaSource>("res://video.mp4");
        // GD.Load can only loaded with (res://) path
        // For both (file system) and (res://) path use this:
        // var source = new FFmpegGodotMediaSource() { Url = "C:/video.mp4" };
        // Then _mediaPlayer.Source = source;
        // Or can be using with _mediaPlayer.Open(source), _mediaPlayer.SetSource(source) as well

        _openFileDialog.FileSelected += path =>
        {
            _newMediaFileLoaded = true;
            _ffmpegPlayer.Open(new FFmpegSource() { Url = path });
        };

        GetViewport().GetWindow().FilesDropped += file =>
        {
            _newMediaFileLoaded = true;
            _ffmpegPlayer.Open(new FFmpegSource() { Url = file[0] });
        };

        // If LoadAsync is enable then Loaded Signal must be used
        _ffmpegPlayer.Loaded += () =>
        {
            if (_isPlaying && !_ffmpegPlayer.AutoPlay)
                _ffmpegPlayer.Play();
            else if (_ffmpegPlayer.AutoPlay)
                _isPlaying = true;

            GetWindow().Title = _ffmpegPlayer.Source.Url.GetFile().GetBaseName();

            GD.Print("Loaded!");
        };

        // Called when media is playing finished, never called when loop is enable
        _ffmpegPlayer.Finished += () => GD.Print("Finished!");

        // Called when media is closed
        _ffmpegPlayer.Closed += () =>
        {
            GetWindow().Title = (string)ProjectSettings.GetSetting("application/config/name");
            GD.Print("Closed!");
        };

        _timeSlider.DragStarted += _ffmpegPlayer.Pause;

        _timeSlider.ValueChanged += value =>
        {
            // There is some fucking bug with this slider :/
            if (_newMediaFileLoaded)
            {
                _newMediaFileLoaded = false;
                return;
            }

            // Changed media position in seconds
            _ffmpegPlayer.Seek(value);
        };

        _timeSlider.DragEnded += valueChanged =>
        {
            if (_isPlaying)
                _ffmpegPlayer.Play();
        };

        _playbackButton.Pressed += () =>
        {
            _isPlaying = !_isPlaying;

            if (_isPlaying)
                _ffmpegPlayer.Play();
            else
                _ffmpegPlayer.Pause();
        };

        _stopButton.Pressed += () =>
        {
            _isPlaying = false;
            _ffmpegPlayer.Stop();
        };

        _loopButton.Toggled += toggle =>
        {
            _ffmpegPlayer.Loop = toggle;
        };

        _settingsWindow.RegisterPlayer(_ffmpegPlayer);

        _controllerButton.Pressed += () =>
        {
            if (_settingsWindow.Visible)
            {
                _settingsWindow.Hide();
                return;
            }

            var mousePos = (Vector2I)GetViewport().GetMousePosition();

            var windowSize = _settingsWindow.Size;

            _settingsWindow.Show();

            _settingsWindow.SetPosition(new Vector2I(mousePos.X - windowSize.X, mousePos.Y - windowSize.Y - 32));
        };

        _fileMenuButton.GetPopup().IdPressed += id =>
        {
            switch (id)
            {
                case 0: // Open
                    _openFileDialog.PopupCentered();
                    break;
                case 1: // Close
                    // Close the media
                    _ffmpegPlayer.Close();
                    break;
                case 2: // Exit
                    GetTree().Quit();
                    break;
            }
        };

        if (_ffmpegPlayer.Source != null && _ffmpegPlayer.AutoPlay)
            _isPlaying = true;

        var args = OS.GetCmdlineArgs();

        if (args.Length > 0 && FileAccess.FileExists(args[0]))
        {
            _ffmpegPlayer.AutoPlay = true;
            _ffmpegPlayer.Open(new FFmpegSource() { Url = args[0] });
        }
    }

    public override void _Process(double delta)
    {
        _timeLabel.Text = TimeSpan.FromSeconds(_ffmpegPlayer.Time).ToString("mm\\:ss\\:fff");

        _durationLabel.Text = TimeSpan.FromSeconds(_ffmpegPlayer.Duration).ToString("mm\\:ss\\:fff");

        _timeSlider.MaxValue = _ffmpegPlayer.Duration;

        _timeSlider.SetValueNoSignal(_ffmpegPlayer.Time);

        _playbackButton.Icon = _ffmpegPlayer.IsPlaying ? _pauseIcon : _playIcon;

        var clockTime = _ffmpegPlayer.ClockTime;

        var videoTime = _ffmpegPlayer.VideoHandler?.Time ?? 0.0;

        var videoDuration = _ffmpegPlayer.VideoHandler?.Duration ?? 0.0;

        var audioTime = _ffmpegPlayer.AudioHandler?.Time ?? 0.0;

        var audioDuration = _ffmpegPlayer.AudioHandler?.Duration ?? 0.0;

        var difference = (_ffmpegPlayer.IsVideoValid && _ffmpegPlayer.IsAudioValid) ? (videoTime - audioTime) : 0.0;

        _debugLabel.Text =
            $"Engine FPS: {Mathf.RoundToInt(Engine.GetFramesPerSecond())}"
            + $"\nClock Time: {clockTime:F3}"
            + $"\nVideo Time | Duration: {videoTime:F3} | {videoDuration:F3}"
            + $"\nAudio Time | Duration: {audioTime:F3} | {audioDuration:F3}"
            + $"\n(Video {(difference == 0.0 ? "=" : difference > 0 ? ">" : "<")} Audio): {Mathf.Abs(difference):F3}"
            + $"\nPlaying: {_ffmpegPlayer.IsPlaying}"
            + $"\nFinished: {_ffmpegPlayer.IsFinished}"
        ;

        if (_showTimeout <= 0.0)
        {
            _ui.Modulate -= Color.FromHsv(0.0f, 0.0f, 0.0f, (float)delta);
            DisplayServer.MouseSetMode(DisplayServer.MouseMode.Hidden);
        }
        else if (!_settingsWindow.Visible)
            _showTimeout -= delta;

        var mousePos = GetGlobalMousePosition();

        if (_lastMousePos != mousePos)
        {
            ShowUI();
            _lastMousePos = mousePos;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey inputEventKey && inputEventKey.IsPressed() && !inputEventKey.IsEcho())
        {
            switch (inputEventKey.PhysicalKeycode)
            {
                case Key.Space:
                    {
                        _isPlaying = !_isPlaying;

                        if (_isPlaying)
                            _ffmpegPlayer.Play();
                        else
                            _ffmpegPlayer.Pause();

                        break;
                    }
                case Key.F11:
                    {
                        ToggleFullScreen();
                        break;
                    }
                case Key.Escape:
                    {
                        GetTree().Quit();
                        break;
                    }
            }
        }

        ShowUI();
    }

    private void ShowUI()
    {
        _showTimeout = 3.0;

        _ui.Modulate = Colors.White;

        DisplayServer.MouseSetMode(DisplayServer.MouseMode.Visible);
    }

    private void ToggleFullScreen()
    {
        if (Engine.IsEmbeddedInEditor())
            return;

        var mode = DisplayServer.WindowGetMode();

        if (mode == DisplayServer.WindowMode.Windowed || mode == DisplayServer.WindowMode.Maximized)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
            _fullScreenButton.Icon = _fullScreenOffIcon;
        }
        else
        {
            DisplayServer.WindowSetMode(_lastWindowMode);
            _fullScreenButton.Icon = _fullScreenOnIcon;
        }

        _lastWindowMode = mode;
    }
}