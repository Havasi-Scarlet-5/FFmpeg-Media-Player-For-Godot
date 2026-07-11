using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using Godot;

namespace FFmpegMediaPlayer.FFmpegGodot;

internal sealed partial class FFmpegAutoLoad : Node
{
    public static ResourcePreloader Preloader { get; private set; }

    public override void _Ready()
    {
        InitializeFFmpegLibrary();

        Preloader = new ResourcePreloader();

        PreloadShaders();
    }

    public static void InitializeFFmpegLibrary()
    {
        FFmpegBinariesHelper.RegisterFFmpegBinaries();
        DynamicallyLoadedBindings.Initialize();
    }

    private static void PreloadShaders()
    {
        Preloader.AddResource("YUVToRGB", ResourceLoader.Load("res://addons/FFmpegMediaPlayer/FFmpegGodot/Shaders/YUVToRGB.gdshader"));
    }
}