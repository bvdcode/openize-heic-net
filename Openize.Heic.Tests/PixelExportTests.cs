/*
 * Openize.HEIC
 * Copyright (c) 2024-2026 Openize Pty Ltd.
 *
 * This file is part of Openize.HEIC.
 *
 * Openize.HEIC is available under Openize license, which is
 * available along with Openize.HEIC sources.
 */

using Openize.Heic.Decoder;

namespace Openize.Heic.Tests
{
    public class PixelExportTests : TestsCore
    {
        [TestCase("nokia/grid_960x640.heic")]
        [TestCase("nokia/overlay_1000x680.heic")]
        [TestCase("gimp_rgb_420_with_alpha.heic")]
        [TestCase("iphone_rotated_and_mirrored.heic")]
        public void CroppedExportsMatchFullRgbaPixels(string filename)
        {
            using FileStream stream = File.OpenRead(Path.Combine(SamplesPath, filename));
            HeicImage image = HeicImage.Load(stream, 2);
            byte[] rgba = image.GetByteArray(PixelFormat.Rgba32);
            Rectangle[] regions =
            [
                new Rectangle(3, 5, 37, 23),
                new Rectangle((int)image.Width - 7, (int)image.Height - 5, 7, 5)
            ];
            PixelFormat[] formats = [PixelFormat.Rgb24, PixelFormat.Rgba32, PixelFormat.Argb32, PixelFormat.Bgra32];
            foreach (Rectangle region in regions)
            {
                foreach (PixelFormat format in formats)
                {
                    int[] channels = GetChannelOrder(format);
                    byte[] expected = new byte[region.Width * region.Height * channels.Length];
                    int[] expectedIntegers = new int[region.Width * region.Height];
                    int target = 0;
                    for (int row = region.Top; row < region.Bottom; row++)
                    {
                        for (int column = region.Left; column < region.Right; column++)
                        {
                            int source = (row * (int)image.Width + column) * 4;
                            foreach (int channel in channels)
                            {
                                expected[target++] = rgba[source + channel];
                            }
                            int packed;
                            switch (format)
                            {
                                case PixelFormat.Rgb24:
                                case PixelFormat.Rgba32:
                                    packed = rgba[source] << 24 | rgba[source + 1] << 16 |
                                        rgba[source + 2] << 8 | rgba[source + 3];
                                    break;
                                case PixelFormat.Argb32:
                                    packed = rgba[source + 3] << 24 | rgba[source] << 16 |
                                        rgba[source + 1] << 8 | rgba[source + 2];
                                    break;
                                case PixelFormat.Bgra32:
                                    packed = rgba[source + 2] << 24 | rgba[source + 1] << 16 |
                                        rgba[source] << 8 | rgba[source + 3];
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException(nameof(format));
                            }
                            expectedIntegers[(row - region.Top) * region.Width + column - region.Left] = packed;
                        }
                    }

                    byte[] actual = image.GetByteArray(format, region);
                    int[] actualIntegers = image.GetInt32Array(format, region);
                    Assert.That(actual.AsSpan().SequenceEqual(expected), Is.True, $"{format}: byte region");
                    Assert.That(actualIntegers.AsSpan().SequenceEqual(expectedIntegers), Is.True, $"{format}: integer region");
                }
            }
        }

        [Test]
        public void ExportedPixelsAreOwnedByTheCaller()
        {
            using FileStream stream = File.OpenRead(Path.Combine(SamplesPath, "nokia/grid_960x640.heic"));
            HeicImage image = HeicImage.Load(stream, 2);
            byte[] original = image.GetByteArray(PixelFormat.Rgba32);
            byte[] exported = image.GetByteArray(PixelFormat.Rgba32);
            Array.Fill(exported, byte.MaxValue);
            byte[] repeated = image.GetByteArray(PixelFormat.Rgba32);
            Assert.That(repeated.AsSpan().SequenceEqual(original), Is.True);
        }

        private static int[] GetChannelOrder(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Rgb24:
                    return [0, 1, 2];
                case PixelFormat.Rgba32:
                    return [0, 1, 2, 3];
                case PixelFormat.Argb32:
                    return [3, 0, 1, 2];
                case PixelFormat.Bgra32:
                    return [2, 1, 0, 3];
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }
    }
}
