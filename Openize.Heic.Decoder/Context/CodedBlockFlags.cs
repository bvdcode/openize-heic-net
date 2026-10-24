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
    internal class CodedBlockFlags
    {
        private const int MinimumTransformLog2Size = 2;
        private readonly byte[,] flags;

        internal CodedBlockFlags(uint width, uint height)
        {
            flags = new byte[width >> MinimumTransformLog2Size, height >> MinimumTransformLog2Size];
        }

        internal bool this[int x, int y, int depth]
        {
            get => (flags[x >> MinimumTransformLog2Size, y >> MinimumTransformLog2Size] & (1 << depth)) != 0;
            set
            {
                int column = x >> MinimumTransformLog2Size;
                int row = y >> MinimumTransformLog2Size;
                int mask = 1 << depth;
                flags[column, row] = (byte)(value ? flags[column, row] | mask : flags[column, row] & ~mask);
            }
        }

        internal void Clear()
        {
            Array.Clear(flags, 0, flags.Length);
        }
    }
}
