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
    public class GridDecodingTests : TestsCore
    {
        [TestCase("nokia/grid_960x640.heic")]
        [TestCase("iphone_photo.heic")]
        [TestCase("iphone_telephoto_with_scaling_list.heic")]
        [TestCase("gimp_rgb_420_with_alpha.heic")]
        public void ParallelDecodingMatchesSequentialPixels(string filename)
        {
            byte[] encoded = File.ReadAllBytes(Path.Combine(SamplesPath, filename));
            using MemoryStream serialStream = new MemoryStream(encoded, false);
            HeicImage serialImage = HeicImage.Load(serialStream, 1);
            byte[] expected = serialImage.GetByteArray(PixelFormat.Rgba32);

            using MemoryStream parallelStream = new MemoryStream(encoded, false);
            HeicImage parallelImage = HeicImage.Load(parallelStream, 4);
            byte[] actual = parallelImage.GetByteArray(PixelFormat.Rgba32);
            byte[] repeated = parallelImage.GetByteArray(PixelFormat.Rgba32);

            Assert.That(actual.AsSpan().SequenceEqual(expected), Is.True);
            Assert.That(repeated.AsSpan().SequenceEqual(expected), Is.True);
            Assert.That(parallelStream.CanRead, Is.True);
        }

        [Test]
        public async Task SharedGridCanBeDecodedConcurrently()
        {
            using FileStream stream = File.OpenRead(Path.Combine(SamplesPath, "nokia/grid_960x640.heic"));
            HeicImage image = HeicImage.Load(stream, 2);
            Task<byte[]> first = Task.Run(() => image.GetByteArray(PixelFormat.Rgba32));
            Task<byte[]> second = Task.Run(() => image.GetByteArray(PixelFormat.Rgba32));
            byte[][] results = await Task.WhenAll(first, second);

            using FileStream referenceStream = File.OpenRead(Path.Combine(SamplesPath, "nokia/grid_960x640.heic"));
            byte[] expected = HeicImage.Load(referenceStream, 1).GetByteArray(PixelFormat.Rgba32);
            foreach (byte[] actual in results)
            {
                Assert.That(actual.AsSpan().SequenceEqual(expected), Is.True);
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void InvalidConcurrencyIsRejected(int threads)
        {
            using MemoryStream stream = new MemoryStream();
            Assert.Throws<ArgumentOutOfRangeException>(() => HeicImage.Load(stream, threads));
        }
    }
}
