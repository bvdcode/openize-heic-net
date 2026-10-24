/*
 * Openize.HEIC
 * Copyright (c) 2024-2026 Openize Pty Ltd.
 * Copyright (c) 2026 Vadim Belov.
 *
 * This file is part of Openize.HEIC.
 *
 * Openize.HEIC is available under Openize license, which is
 * available along with Openize.HEIC sources.
 */

using System;

namespace Openize.Heic.Decoder
{
    internal class RgbaOrientation
    {
        private readonly int sourceWidth;
        private readonly int sourceHeight;
        private readonly int origin;
        private readonly int columnStep;
        private readonly int rowStep;
        private readonly bool identity;

        internal int Width { get; }
        internal int Height { get; }

        internal RgbaOrientation(int width, int height, int rotation, int mirror)
        {
            sourceWidth = width;
            sourceHeight = height;
            identity = rotation == 0 && mirror == 0;
            switch (rotation)
            {
                case 0:
                    Width = width;
                    Height = height;
                    origin = 0;
                    columnStep = RgbaImage.BytesPerPixel;
                    rowStep = width * RgbaImage.BytesPerPixel;
                    break;
                case 1:
                    Width = height;
                    Height = width;
                    origin = (width - 1) * height * RgbaImage.BytesPerPixel;
                    columnStep = -height * RgbaImage.BytesPerPixel;
                    rowStep = RgbaImage.BytesPerPixel;
                    break;
                case 2:
                    Width = width;
                    Height = height;
                    origin = (width * height - 1) * RgbaImage.BytesPerPixel;
                    columnStep = -RgbaImage.BytesPerPixel;
                    rowStep = -width * RgbaImage.BytesPerPixel;
                    break;
                case 3:
                    Width = height;
                    Height = width;
                    origin = (height - 1) * RgbaImage.BytesPerPixel;
                    columnStep = height * RgbaImage.BytesPerPixel;
                    rowStep = -RgbaImage.BytesPerPixel;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rotation));
            }
            switch (mirror)
            {
                case 0:
                    break;
                case 1:
                    if ((rotation & 1) == 0)
                    {
                        origin += (height - 1) * rowStep;
                        rowStep = -rowStep;
                    }
                    else
                    {
                        origin += (width - 1) * columnStep;
                        columnStep = -columnStep;
                    }
                    break;
                case 2:
                    if ((rotation & 1) == 0)
                    {
                        origin += (width - 1) * columnStep;
                        columnStep = -columnStep;
                    }
                    else
                    {
                        origin += (height - 1) * rowStep;
                        rowStep = -rowStep;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mirror));
            }
        }

        internal void CopyTo(RgbaImage source, RgbaImage destination, Rectangle bounds)
        {
            if (identity)
            {
                source.CopyTo(destination, bounds);
                return;
            }
            int width = Math.Min(bounds.Width, sourceWidth - bounds.Left);
            int height = Math.Min(bounds.Height, sourceHeight - bounds.Top);
            int firstTarget = origin + bounds.Left * columnStep + bounds.Top * rowStep;
            for (int row = 0; row < height; row++)
            {
                int sourceIndex = row * source.Stride;
                int targetIndex = firstTarget + row * rowStep;
                for (int column = 0; column < width; column++)
                {
                    destination.Pixels[targetIndex] = source.Pixels[sourceIndex];
                    destination.Pixels[targetIndex + 1] = source.Pixels[sourceIndex + 1];
                    destination.Pixels[targetIndex + 2] = source.Pixels[sourceIndex + 2];
                    destination.Pixels[targetIndex + 3] = source.Pixels[sourceIndex + 3];
                    sourceIndex += RgbaImage.BytesPerPixel;
                    targetIndex += columnStep;
                }
            }
        }
    }
}
