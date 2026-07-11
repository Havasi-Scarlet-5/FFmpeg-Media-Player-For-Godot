using System;
using FFmpeg.AutoGen.Abstractions;
using Godot;

namespace FFmpegMediaPlayer.FFmpegGodot;

public partial class FFmpegYUVTextureHandler : RefCounted
{
    private TextureRect _texture;

    private readonly FFmpegVideoDecoder _decoder;

    private ShaderMaterial _shaderMaterial;

    public FFmpegYUVTextureHandler(TextureRect texture, FFmpegVideoDecoder decoder)
    {
        _texture = texture;

        _decoder = decoder;

        _shaderMaterial = new ShaderMaterial
        {
            Shader = (Shader)FFmpegAutoLoad.Preloader.GetResource("YUVToRGB")
        };

        _texture.Material = _shaderMaterial;
    }

    public void SetHue(float value)
    {
        _shaderMaterial?.SetShaderParameter("hue", value);
    }

    public void SetSaturation(float value)
    {
        _shaderMaterial?.SetShaderParameter("saturation", value);
    }

    public void SetLightness(float value)
    {
        _shaderMaterial?.SetShaderParameter("lightness", value);
    }

    public void SetContrast(float value)
    {
        _shaderMaterial?.SetShaderParameter("contrast", value);
    }

    public void SetTintColor(Color value)
    {
        _shaderMaterial?.SetShaderParameter("tint_color", value);
    }

    public void SetChromaKeyEnable(bool value)
    {
        _shaderMaterial?.SetShaderParameter("chroma_key_enable", value);
    }

    public void SetChromaKeyColor(Color value)
    {
        _shaderMaterial?.SetShaderParameter("chroma_key_color", value);
    }

    public void SetChromaKeyThreshold(float value)
    {
        _shaderMaterial?.SetShaderParameter("chroma_key_threshold", value);
    }

    public void SetChromaKeySmoothness(float value)
    {
        _shaderMaterial?.SetShaderParameter("chroma_key_smoothness", value);
    }

    private static float[,] GetColorSpaceMatrix(AVColorSpace colorSpace)
    {
        return colorSpace switch
        {
            // BT.601 (PAL/NTSC SDTV)
            AVColorSpace.AVCOL_SPC_BT470BG => new float[,]
            {
                { 1.0f,  0.0f,       1.402f    },
                { 1.0f, -0.344136f, -0.714136f },
                { 1.0f,  1.772f,     0.0f      }
            },
            AVColorSpace.AVCOL_SPC_SMPTE170M => new float[,]
            {
                { 1.0f,  0.0f,       1.402f    },
                { 1.0f, -0.344136f, -0.714136f },
                { 1.0f,  1.772f,     0.0f      }
            },
            // SMPTE 240M
            AVColorSpace.AVCOL_SPC_SMPTE240M => new float[,]
            {
                { 1.0f,  0.0f,       1.575f    },
                { 1.0f, -0.225f,    -0.500f    },
                { 1.0f,  1.826f,     0.0f      }
            },
            // BT.2020 (UHD/4K/8K)
            AVColorSpace.AVCOL_SPC_BT2020_NCL => new float[,]
            {
                { 1.0f,  0.0f,       1.4746f   },
                { 1.0f, -0.164553f, -0.571353f },
                { 1.0f,  1.8814f,    0.0f      }
            },
            AVColorSpace.AVCOL_SPC_BT2020_CL => new float[,]
            {
                { 1.0f,  0.0f,       1.4746f   },
                { 1.0f, -0.164553f, -0.571353f },
                { 1.0f,  1.8814f,    0.0f      }
            },
            // BT.709 (HDTV) - Default
            _ => new float[,]
            {
                { 1.0f,  0.0f,       1.5748f   },   // R
                { 1.0f, -0.1873f,   -0.4681f   },   // G
                { 1.0f,  1.8556f,    0.0f      }    // B
            }
        };
    }

    private ImageTexture _yTexture;

    private ImageTexture _uTexture;

    private ImageTexture _vTexture;

    private Image _yImage;

    private Image _uImage;

    private Image _vImage;

    private bool _texturesCreated;

    public void UpdateYUVTexture(
        int yWidth,
        int uWidth,
        int vWidth,
        int height,
        int padding,
        ReadOnlySpan<byte> yData,
        ReadOnlySpan<byte> uData,
        ReadOnlySpan<byte> vData
    )
    {
        if (!_texturesCreated)
        {
            _yImage = Image.CreateEmpty(yWidth, height, false, Image.Format.R8);

            _uImage = Image.CreateEmpty(uWidth, height / 2, false, Image.Format.R8);

            _vImage = Image.CreateEmpty(vWidth, height / 2, false, Image.Format.R8);

            _yTexture = ImageTexture.CreateFromImage(_yImage);

            _uTexture = ImageTexture.CreateFromImage(_uImage);

            _vTexture = ImageTexture.CreateFromImage(_vImage);

            _shaderMaterial?.SetShaderParameter("tex_y", _yTexture);

            _shaderMaterial?.SetShaderParameter("tex_u", _uTexture);

            _shaderMaterial?.SetShaderParameter("tex_v", _vTexture);

            var matrix = GetColorSpaceMatrix(_decoder.ColorSpace);

            var basis = new Basis(
                new Vector3(matrix[0, 0], matrix[1, 0], matrix[2, 0]),
                new Vector3(matrix[0, 1], matrix[1, 1], matrix[2, 1]),
                new Vector3(matrix[0, 2], matrix[1, 2], matrix[2, 2])
            );

            _shaderMaterial?.SetShaderParameter("color_space_matrix", basis);

            var widthScale = padding > 0 ? 1.0f * (yWidth - padding - 1) / yWidth : 1.0f;

            _shaderMaterial?.SetShaderParameter("texture_scale", new Vector2(widthScale, 1.0f));

            _texturesCreated = true;
        }

        _yImage.SetData(yWidth, height, false, Image.Format.R8, yData);

        _uImage.SetData(uWidth, height / 2, false, Image.Format.R8, uData);

        _vImage.SetData(vWidth, height / 2, false, Image.Format.R8, vData);

        _yTexture.Update(_yImage);

        _uTexture.Update(_uImage);

        _vTexture.Update(_vImage);
    }
}