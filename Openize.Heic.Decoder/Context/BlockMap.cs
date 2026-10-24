/*
 * Openize.HEIC
 * Copyright (c) 2024-2026 Openize Pty Ltd.
 *
 * This file is part of Openize.HEIC.
 *
 * Openize.HEIC is available under Openize license, which is
 * available along with Openize.HEIC sources.
 */

using System;

namespace Openize.Heic.Decoder
{
    internal class BlockMap<T>
    {
        private const int BlockLog2Size = 2;
        private readonly T[,] values;

        internal BlockMap(uint width, uint height)
        {
            values = new T[width >> BlockLog2Size, height >> BlockLog2Size];
        }

        internal T this[int x, int y] => values[x >> BlockLog2Size, y >> BlockLog2Size];

        internal void Fill(int x, int y, int size, T value)
        {
            int right = (x + size) >> BlockLog2Size;
            int bottom = (y + size) >> BlockLog2Size;
            for (int column = x >> BlockLog2Size; column < right; column++)
            {
                for (int row = y >> BlockLog2Size; row < bottom; row++)
                {
                    values[column, row] = value;
                }
            }
        }

        internal void Clear()
        {
            Array.Clear(values, 0, values.Length);
        }
    }
}
