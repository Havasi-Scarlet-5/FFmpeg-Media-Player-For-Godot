using Godot;

namespace FFmpegMediaPlayer.FFmpegGodot;

/// <summary>
/// Can be (video - audio) file or link.
/// </summary>
[GlobalClass, Icon("res://addons/FFmpegMediaPlayer/FFmpegGodot/Icons/Source.svg")]
public partial class FFmpegSource : Resource
{
    /// <summary>
    /// Url or file path.
    /// </summary>
    [Export]
    public string Url { get; set; }
}