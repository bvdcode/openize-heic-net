/*
 * Openize.HEIC
 * Copyright (c) 2024-2026 Openize Pty Ltd.
 *
 * This file is part of Openize.HEIC.
 *
 * Openize.HEIC is available under Openize license, which is
 * available along with Openize.HEIC sources.
 */

using System;

namespace Openize.Heic.Decoder
{
    internal class RgbaImage
    {
        internal const int BytesPerPixel = 4;

        internal int Width { get; }
        internal int Height { get; }
        internal int Stride { get; }
        internal byte[] Pixels { get; }

        internal RgbaImage(int width, int height)
        {
            Width = width;
            Height = height;
            Stride = checked(width * BytesPerPixel);
            Pixels = new byte[checked(Stride * height)];
        }

        internal void CopyTo(RgbaImage destination, Rectangle bounds)
        {
            int width = Math.Min(bounds.Width, destination.Width - bounds.Left);
            int height = Math.Min(bounds.Height, destination.Height - bounds.Top);
            if (width <= 0 || height <= 0)
            {
                return;
            }

            int count = checked(width * BytesPerPixel);
            for (int row = 0; row < height; row++)
            {
                Buffer.BlockCopy(Pixels, row * Stride, destination.Pixels,
                    (bounds.Top + row) * destination.Stride + bounds.Left * BytesPerPixel, count);
            }
        }

        internal byte[] GetByteArray(PixelFormat format, Rectangle bounds)
        {
            int bytesPerPixel;
            switch (format)
            {
                case PixelFormat.Rgb24:
                    bytesPerPixel = 3;
                    break;
                case PixelFormat.Rgba32:
                case PixelFormat.Argb32:
                case PixelFormat.Bgra32:
                    bytesPerPixel = BytesPerPixel;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }

            byte[] output;
            if (bytesPerPixel == BytesPerPixel && bounds.Left == 0 && bounds.Top == 0 &&
                bounds.Width == Width && bounds.Height == Height)
            {
                output = Pixels;
            }
            else
            {
                int stride = checked(bounds.Width * bytesPerPixel);
                output = new byte[checked(stride * bounds.Height)];
                for (int row = 0; row < bounds.Height; row++)
                {
                    int source = (bounds.Top + row) * Stride + bounds.Left * BytesPerPixel;
                    int target = row * stride;
                    if (bytesPerPixel == BytesPerPixel)
                    {
                        Buffer.BlockCopy(Pixels, source, output, target, stride);
                    }
                    else
                    {
                        for (int column = 0; column < bounds.Width; column++)
                        {
                            output[target++] = Pixels[source++];
                            output[target++] = Pixels[source++];
                            output[target++] = Pixels[source++];
                            source++;
                        }
                    }
                }
            }

            switch (format)
            {
                case PixelFormat.Rgb24:
                case PixelFormat.Rgba32:
                    break;
                case PixelFormat.Argb32:
                    for (int index = 0; index < output.Length; index += BytesPerPixel)
                    {
                        byte alpha = output[index + 3];
                        output[index + 3] = output[index + 2];
                        output[index + 2] = output[index + 1];
                        output[index + 1] = output[index];
                        output[index] = alpha;
                    }
                    break;
                case PixelFormat.Bgra32:
                    for (int index = 0; index < output.Length; index += BytesPerPixel)
                    {
                        byte red = output[index];
                        output[index] = output[index + 2];
                        output[index + 2] = red;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
            return output;
        }

        internal int[] GetInt32Array(PixelFormat format, Rectangle bounds)
        {
            int[] output = new int[checked(bounds.Width * bounds.Height)];
            int target = 0;
            for (int row = bounds.Top; row < bounds.Bottom; row++)
            {
                int source = row * Stride + bounds.Left * BytesPerPixel;
                for (int column = 0; column < bounds.Width; column++)
                {
                    int red = Pixels[source++];
                    int green = Pixels[source++];
                    int blue = Pixels[source++];
                    int alpha = Pixels[source++];
                    switch (format)
                    {
                        case PixelFormat.Rgb24:
                        case PixelFormat.Rgba32:
                            output[target++] = red << 24 | green << 16 | blue << 8 | alpha;
                            break;
                        case PixelFormat.Argb32:
                            output[target++] = alpha << 24 | red << 16 | green << 8 | blue;
                            break;
                        case PixelFormat.Bgra32:
                            output[target++] = blue << 24 | green << 16 | red << 8 | alpha;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(format));
                    }
                }
            }
            return output;
        }
    }
}
