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
    public class ScanPositionTests
    {
        [Test, Combinatorial]
        public void CoordinatesMapToHierarchicalScan([Values(2, 3, 4, 5)] int log2Size,
            [Values(0, 1, 2)] int scanIndex)
        {
            Scans.Initialize();
            int size = 1 << log2Size;
            bool[,] visited = new bool[size, size];
            byte[][] subBlocks = Scans.ScanOrder[log2Size - 2][scanIndex];
            byte[][] coefficients = Scans.ScanOrder[2][scanIndex];
            for (int position = 0; position < size * size; position++)
            {
                int subBlock = position >> 4;
                int coefficient = position & 15;
                int x = (subBlocks[subBlock][0] << 2) + coefficients[coefficient][0];
                int y = (subBlocks[subBlock][1] << 2) + coefficients[coefficient][1];
                Assert.That(visited[x, y], Is.False);
                visited[x, y] = true;
                Assert.That(Scans.GetScanPosition(log2Size - 2, scanIndex, x >> 2, y >> 2), Is.EqualTo(subBlock));
                Assert.That(Scans.GetScanPosition(2, scanIndex, x & 3, y & 3), Is.EqualTo(coefficient));
            }
        }
    }
}
