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
    public class PointMapTests
    {
        [Test]
        public void AlignedWritesMatchDenseStorageIncludingUnwrittenNeighbourPixels()
        {
            const int Width = 32;
            const int Height = 64;
            PointMap<int> map = new PointMap<int>(Width, Height);
            int[,] expected = new int[Width, Height];
            Random random = new Random(471829);
            for (int iteration = 0; iteration < 200; iteration++)
            {
                int x = random.Next(Width / 4) * 4;
                int y = random.Next(Height / 4) * 4;
                int value = random.Next();
                map[x, y] = value;
                expected[x, y] = value;
                for (int column = 0; column < Width; column++)
                {
                    for (int row = 0; row < Height; row++)
                    {
                        Assert.That(map[column, row], Is.EqualTo(expected[column, row]));
                    }
                }
            }
            map.Clear();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Assert.That(map[x, y], Is.Zero);
                }
            }
            map[4, 8] = 73;
            Assert.That(map[4, 8], Is.EqualTo(73));
            Assert.That(map[5, 8], Is.Zero);
            Assert.That(map[4, 9], Is.Zero);
            Assert.That(map[7, 11], Is.Zero);
            Assert.That(map[8, 8], Is.Zero);
        }

        [TestCase(1, 4)]
        [TestCase(4, 7)]
        public void UnalignedWritesAreRejected(int x, int y)
        {
            PointMap<int> map = new PointMap<int>(16, 16);
            Assert.Throws<ArgumentException>(() => map[x, y] = 42);
        }
    }
}
