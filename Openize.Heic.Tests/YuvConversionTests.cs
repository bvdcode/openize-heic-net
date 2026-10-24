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
    public class YuvConversionTests : TestsCore
    {
        [Test, Combinatorial]
        public void ConversionMatchesPerPixelReference([Values(8, 10, 12)] int lumaDepth,
            [Values(8, 10, 12)] int chromaDepth, [Values(0, 1, 2, 3)] int chromaFormat,
            [Values(false, true)] bool fullRange, [Values(1, 6, 9)] int matrix)
        {
            const int Width = 37;
            const int Height = 35;
            using FileStream stream = File.OpenRead(Path.Combine(SamplesPath, "nokia/grid_960x640.heic"));
            HeicImage image = HeicImage.Load(stream, 1);
            HeicImageFrame frame = image.AllFrames.Values.First(item => item.ImageType == ImageFrameType.hvc1);
            seq_parameter_set_rbsp sps = frame.hvcConfig.SPS;
            sps.pic_width_in_luma_samples = Width;
            sps.pic_height_in_luma_samples = Height;
            sps.chroma_format_idc = (uint)chromaFormat;
            sps.separate_colour_plane_flag = false;
            sps.bit_depth_luma_minus8 = (byte)(lumaDepth - 8);
            sps.bit_depth_chroma_minus8 = (byte)(chromaDepth - 8);
            sps.vui_parameters.video_full_range_flag = fullRange;
            sps.vui_parameters.matrix_coeffs = (byte)matrix;
            int components = chromaFormat == 0 ? 1 : 3;
            ushort[][,] source = new ushort[components][,];
            byte[][,] byteSource = new byte[components][,];
            bool highColorRange = lumaDepth > 8 || chromaDepth > 8;
            if (highColorRange)
            {
                frame.rawPixelsHighColorRange = source;
                frame.rawPixels = null;
            }
            else
            {
                frame.rawPixels = byteSource;
                frame.rawPixelsHighColorRange = null;
            }

            Random random = new Random(24681357);
            for (int component = 0; component < components; component++)
            {
                int subWidth = component == 0 ? 1 : sps.SubWidthC;
                int subHeight = component == 0 ? 1 : sps.SubHeightC;
                int width = (Width + subWidth - 1) / subWidth;
                int height = (Height + subHeight - 1) / subHeight;
                int depth = component == 0 ? lumaDepth : chromaDepth;
                int limit = 1 << depth;
                int[] boundaries = [0, 1, limit / 2 - 1, limit / 2, limit - 2, limit - 1,
                    16 << (depth - 8), 235 << (depth - 8), 240 << (depth - 8)];
                source[component] = new ushort[width, height];
                if (!highColorRange)
                {
                    byteSource[component] = new byte[width, height];
                }
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int position = x * height + y;
                        ushort value = (ushort)(position < boundaries.Length ? boundaries[position] : random.Next(limit));
                        source[component][x, y] = value;
                        if (!highColorRange)
                        {
                            byteSource[component][x, y] = (byte)value;
                        }
                    }
                }
            }

            byte[] expected = ConvertReference(source, sps, matrix);
            TileDecoderWorkspace workspace = new TileDecoderWorkspace();
            RgbaImage previous = workspace.RentRgbaImage(Width, Height);
            Array.Fill(previous.Pixels, byte.MaxValue);
            workspace.ReturnRgbaImage(previous);
            byte[] actual = new YuvConverter(frame).GetRgbaImage(workspace).Pixels;
            Assert.That(actual.AsSpan().SequenceEqual(expected), Is.True,
                $"Depth {lumaDepth}/{chromaDepth}, chroma {chromaFormat}, full range {fullRange}, matrix {matrix}");
        }

        private static byte[] ConvertReference(ushort[][,] source, seq_parameter_set_rbsp sps, int matrix)
        {
            double kr;
            double kb;
            switch (matrix)
            {
                case 1:
                    kr = 0.2126;
                    kb = 0.0722;
                    break;
                case 6:
                    kr = 0.299;
                    kb = 0.114;
                    break;
                case 9:
                    kr = 0.2627;
                    kb = 0.0593;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(matrix));
            }
            double kg = 1 - kr - kb;
            double redFactor = 2 * (1 - kr);
            double greenCrFactor = 2 * kr * (1 - kr) / kg;
            double greenCbFactor = 2 * kb * (1 - kb) / kg;
            double blueFactor = 2 * (1 - kb);
            int width = (int)sps.pic_width_in_luma_samples;
            int height = (int)sps.pic_height_in_luma_samples;
            byte[] pixels = new byte[width * height * RgbaImage.BytesPerPixel];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double luma = source[0][x, y];
                    if (!sps.vui_parameters.video_full_range_flag)
                    {
                        luma = (255.0 / 219) * (luma - (16 << (sps.BitDepthY - 8)));
                    }
                    double red = luma;
                    double green = luma;
                    double blue = luma;
                    if (sps.ChromaArrayType != 0)
                    {
                        double cb = source[1][x / sps.SubWidthC, y / sps.SubHeightC] - (1 << (sps.BitDepthC - 1));
                        double cr = source[2][x / sps.SubWidthC, y / sps.SubHeightC] - (1 << (sps.BitDepthC - 1));
                        if (!sps.vui_parameters.video_full_range_flag)
                        {
                            cb = (255.0 / 224) * cb;
                            cr = (255.0 / 224) * cr;
                        }
                        luma *= 256.0 / (1 << sps.BitDepthY);
                        cb *= 256.0 / (1 << sps.BitDepthC);
                        cr *= 256.0 / (1 << sps.BitDepthC);
                        red = luma + redFactor * cr;
                        green = luma - greenCrFactor * cr - greenCbFactor * cb;
                        blue = luma + blueFactor * cb;
                    }
                    int index = (y * width + x) * RgbaImage.BytesPerPixel;
                    pixels[index] = (byte)Math.Clamp((int)red, 0, 255);
                    pixels[index + 1] = (byte)Math.Clamp((int)green, 0, 255);
                    pixels[index + 2] = (byte)Math.Clamp((int)blue, 0, 255);
                    pixels[index + 3] = 255;
                }
            }
            return pixels;
        }
    }
}
