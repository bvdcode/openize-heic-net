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
    public class TileRgbaStorageTests : TestsCore
    {
        [Test]
        public void MissingPlanesClearPixelsFromThePreviousTile()
        {
            using FileStream stream = File.OpenRead(Path.Combine(SamplesPath, "nokia/grid_960x640.heic"));
            HeicImage image = HeicImage.Load(stream, 1);
            HeicImageFrame frame = image.AllFrames.Values.First(item => item.ImageType == ImageFrameType.hvc1);
            frame.hvcConfig.SPS.pic_width_in_luma_samples = 37;
            frame.hvcConfig.SPS.pic_height_in_luma_samples = 29;
            TileDecoderWorkspace workspace = new TileDecoderWorkspace();
            RgbaImage previous = workspace.RentRgbaImage(37, 29);
            Array.Fill(previous.Pixels, byte.MaxValue);
            workspace.ReturnRgbaImage(previous);
            RgbaImage actual = new YuvConverter(frame).GetRgbaImage(workspace);
            Assert.That(actual, Is.SameAs(previous));
            Assert.That(actual.Pixels, Is.All.Zero);
        }

        [Test]
        public void BuffersRemainExclusiveUntilReturnedAndRequireMatchingGeometry()
        {
            TileDecoderWorkspace workspace = new TileDecoderWorkspace();
            RgbaImage first = workspace.RentRgbaImage(37, 29);
            RgbaImage second = workspace.RentRgbaImage(37, 29);
            Assert.That(second, Is.Not.SameAs(first));
            first.Pixels[0] = 73;
            second.Pixels[0] = 29;
            workspace.ReturnRgbaImage(first);
            RgbaImage reused = workspace.RentRgbaImage(37, 29);
            Assert.That(reused, Is.SameAs(first));
            Assert.That(second.Pixels[0], Is.EqualTo(29));
            RgbaImage pending = workspace.RentRgbaImage(37, 29);
            Assert.That(pending, Is.Not.SameAs(first));
            Assert.That(pending, Is.Not.SameAs(second));
            workspace.ReturnRgbaImage(first);
            RgbaImage resized = workspace.RentRgbaImage(29, 37);
            Assert.That(resized, Is.Not.SameAs(first));
            Assert.That(resized.Width, Is.EqualTo(29));
            Assert.That(resized.Height, Is.EqualTo(37));
            Assert.That(resized.Pixels[0], Is.Zero);
        }
    }
}
