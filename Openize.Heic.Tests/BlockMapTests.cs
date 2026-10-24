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
    public class BlockMapTests
    {
        [Test]
        public void MixedBlockSizesMatchDensePixelMapAtEveryNeighbourCoordinate()
        {
            const int Width = 64;
            const int Height = 128;
            BlockMap<int> map = new BlockMap<int>(Width, Height);
            int[,] expected = new int[Width, Height];
            Random random = new Random(975312);
            for (int iteration = 0; iteration < 200; iteration++)
            {
                int size = 1 << random.Next(2, 7);
                int x = random.Next(Width / size) * size;
                int y = random.Next(Height / size) * size;
                int value = random.Next();
                map.Fill(x, y, size, value);
                for (int column = x; column < x + size; column++)
                {
                    for (int row = y; row < y + size; row++)
                    {
                        expected[column, row] = value;
                    }
                }
                for (int column = 0; column < Width; column++)
                {
                    for (int row = 0; row < Height; row++)
                    {
                        Assert.That(map[column, row], Is.EqualTo(expected[column, row]));
                    }
                }
            }
            map.Clear();
            for (int column = 0; column < Width; column++)
            {
                for (int row = 0; row < Height; row++)
                {
                    Assert.That(map[column, row], Is.Zero);
                }
            }
            map.Fill(4, 8, 4, 73);
            Assert.That(map[7, 11], Is.EqualTo(73));
            Assert.That(map[3, 8], Is.Zero);
            Assert.That(map[8, 8], Is.Zero);
            Assert.That(map[4, 7], Is.Zero);
            Assert.That(map[4, 12], Is.Zero);
        }
    }
}
