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

using Openize.Heic.Decoder;

namespace Openize.Heic.Tests
{
    public class RgbaOrientationTests
    {
        [Test, Combinatorial]
        public void PartialGridTilesMatchFullImageOrientation(
            [Values(0, 1, 2, 3)] int rotation, [Values(0, 1, 2)] int mirror,
            [Values(1, 2, 7)] int width, [Values(1, 3, 9)] int height)
        {
            const int TileWidth = 4;
            const int TileHeight = 4;
            RgbaImage source = new RgbaImage(width, height);
            Random random = new Random(147258);
            random.NextBytes(source.Pixels);
            RgbaOrientation orientation = new RgbaOrientation(width, height, rotation, mirror);
            RgbaImage actual = new RgbaImage(orientation.Width, orientation.Height);
            for (int top = 0; top < height; top += TileHeight)
            {
                for (int left = 0; left < width; left += TileWidth)
                {
                    RgbaImage tile = new RgbaImage(TileWidth, TileHeight);
                    Array.Fill(tile.Pixels, (byte)241);
                    for (int y = 0; y < Math.Min(TileHeight, height - top); y++)
                    {
                        for (int x = 0; x < Math.Min(TileWidth, width - left); x++)
                        {
                            Buffer.BlockCopy(source.Pixels, (top + y) * source.Stride + (left + x) * 4,
                                tile.Pixels, y * tile.Stride + x * 4, 4);
                        }
                    }
                    orientation.CopyTo(tile, actual, new Rectangle(left, top, TileWidth, TileHeight));
                }
            }
            byte[] expected = new byte[source.Pixels.Length];
            int outputWidth = orientation.Width;
            int outputHeight = orientation.Height;
            for (int row = 0; row < outputHeight; row++)
            {
                for (int column = 0; column < outputWidth; column++)
                {
                    int oldColumn;
                    int oldRow;
                    switch (rotation)
                    {
                        case 0:
                            oldColumn = column;
                            oldRow = row;
                            break;
                        case 1:
                            oldColumn = outputHeight - row - 1;
                            oldRow = column;
                            break;
                        case 2:
                            oldColumn = outputWidth - column - 1;
                            oldRow = outputHeight - row - 1;
                            break;
                        case 3:
                            oldColumn = row;
                            oldRow = outputWidth - column - 1;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(rotation));
                    }
                    switch (mirror)
                    {
                        case 0:
                            break;
                        case 1:
                            if (rotation == 0 || rotation == 2)
                            {
                                oldRow = outputHeight - oldRow - 1;
                            }
                            else
                            {
                                oldColumn = outputHeight - oldColumn - 1;
                            }
                            break;
                        case 2:
                            if (rotation == 0 || rotation == 2)
                            {
                                oldColumn = outputWidth - oldColumn - 1;
                            }
                            else
                            {
                                oldRow = outputWidth - oldRow - 1;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(mirror));
                    }
                    Buffer.BlockCopy(source.Pixels, oldRow * source.Stride + oldColumn * 4,
                        expected, (row * outputWidth + column) * 4, 4);
                }
            }
            Assert.That(actual.Pixels, Is.EqualTo(expected));
        }
    }
}
