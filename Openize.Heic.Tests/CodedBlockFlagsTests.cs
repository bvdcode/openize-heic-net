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
    public class CodedBlockFlagsTests
    {
        [Test]
        public void NestedDepthsAndAdjacentBlocksRemainIndependent()
        {
            const int Width = 64;
            const int Height = 128;
            const int DepthCount = 5;
            CodedBlockFlags flags = new CodedBlockFlags(Width, Height);
            bool[,,] expected = new bool[Width / 4, Height / 4, DepthCount];
            Random random = new Random(468231);
            for (int iteration = 0; iteration < 10000; iteration++)
            {
                int column = random.Next(Width / 4);
                int row = random.Next(Height / 4);
                int depth = random.Next(DepthCount);
                bool value = random.Next(2) != 0;
                flags[column * 4, row * 4, depth] = value;
                expected[column, row, depth] = value;
                for (int level = 0; level < DepthCount; level++)
                {
                    Assert.That(flags[column * 4, row * 4, level], Is.EqualTo(expected[column, row, level]));
                }
            }
            for (int column = 0; column < Width / 4; column++)
            {
                for (int row = 0; row < Height / 4; row++)
                {
                    for (int depth = 0; depth < DepthCount; depth++)
                    {
                        Assert.That(flags[column * 4, row * 4, depth], Is.EqualTo(expected[column, row, depth]));
                    }
                }
            }
            flags.Clear();
            for (int column = 0; column < Width / 4; column++)
            {
                for (int row = 0; row < Height / 4; row++)
                {
                    for (int depth = 0; depth < DepthCount; depth++)
                    {
                        Assert.That(flags[column * 4, row * 4, depth], Is.False);
                    }
                }
            }
            flags[4, 8, 3] = true;
            Assert.That(flags[4, 8, 3], Is.True);
            Assert.That(flags[4, 8, 2], Is.False);
            Assert.That(flags[4, 12, 3], Is.False);
        }
    }
}
