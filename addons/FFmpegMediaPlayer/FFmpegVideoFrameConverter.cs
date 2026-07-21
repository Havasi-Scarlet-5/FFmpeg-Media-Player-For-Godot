using System;
using System.Drawing;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegMediaPlayer;

public sealed unsafe class FFmpegVideoFrameConverter : IDisposable
{
    private byte* _convertedFrameBuffer;

    private readonly Size _destinationSize;

    private readonly AVPixelFormat _destinationPixelFormat;

    private readonly byte_ptr4 _dstData;

    private readonly int4 _dstLinesize;

    private SwsContext* _pConvertContext;

    public FFmpegVideoFrameConverter(
        Size sourceSize,
        AVPixelFormat sourcePixelFormat,
        Size destinationSize,
        AVPixelFormat destinationPixelFormat
    )
    {
        _destinationSize = destinationSize;

        _destinationPixelFormat = destinationPixelFormat;

        _pConvertContext = ffmpeg.sws_getContext(
            sourceSize.Width,
            sourceSize.Height,
            sourcePixelFormat,
            destinationSize.Width,
            destinationSize.Height,
            destinationPixelFormat,
            (int)SwsFlags.SWS_FAST_BILINEAR,
            null,
            null,
            null
        );

        if (_pConvertContext == null)
        {
            FFmpegLogger.LogErr(this, "Could not initialize the conversion context.");
            return;
        }

        var convertedFrameBufferSize = ffmpeg.av_image_get_buffer_size(
            destinationPixelFormat,
            destinationSize.Width,
            destinationSize.Height,
            1
        );

        _convertedFrameBuffer = (byte*)ffmpeg.av_malloc((ulong)convertedFrameBufferSize);

        _dstData = new byte_ptr4();

        _dstLinesize = new int4();

        ffmpeg.av_image_fill_arrays(
            ref _dstData,
            ref _dstLinesize,
            _convertedFrameBuffer,
            destinationPixelFormat,
            destinationSize.Width,
            destinationSize.Height,
            1
        );
    }

    public void Dispose()
    {
        if (_convertedFrameBuffer != null)
        {
            ffmpeg.av_free(_convertedFrameBuffer);
            _convertedFrameBuffer = null;
        }

        if (_pConvertContext != null)
        {
            ffmpeg.sws_freeContext(_pConvertContext);
            _pConvertContext = null;
        }
    }

    public AVFrame Convert(AVFrame sourceFrame)
    {
        ffmpeg.sws_scale(
            _pConvertContext,
            sourceFrame.data,
            sourceFrame.linesize,
            0,
            sourceFrame.height,
            _dstData,
            _dstLinesize
        );

        var data = new byte_ptr8();

        data.UpdateFrom(_dstData);

        var linesize = new int8();

        linesize.UpdateFrom(_dstLinesize);

        return new AVFrame
        {
            data = data,
            linesize = linesize,
            width = _destinationSize.Width,
            height = _destinationSize.Height,
            format = (int)_destinationPixelFormat
        };
    }
}