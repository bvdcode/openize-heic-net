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

using System;

namespace Openize.Heic.Decoder
{
    internal class PointMap<T>
    {
        private const int PointLog2Spacing = 2;
        private const int CoordinateMask = (1 << PointLog2Spacing) - 1;
        private readonly T[,] values;

        internal PointMap(uint width, uint height)
        {
            values = new T[width >> PointLog2Spacing, height >> PointLog2Spacing];
        }

        internal T this[int x, int y]
        {
            get
            {
                T value = values[x >> PointLog2Spacing, y >> PointLog2Spacing];
                if (((x | y) & CoordinateMask) != 0)
                {
                    return default(T);
                }
                return value;
            }
            set
            {
                if (((x | y) & CoordinateMask) != 0)
                {
                    throw new ArgumentException("Syntax coordinates must be aligned to the minimum block size.");
                }
                values[x >> PointLog2Spacing, y >> PointLog2Spacing] = value;
            }
        }

        internal void Clear()
        {
            Array.Clear(values, 0, values.Length);
        }
    }
}
