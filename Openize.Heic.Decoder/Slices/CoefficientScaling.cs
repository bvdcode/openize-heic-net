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

namespace Openize.Heic.Decoder
{
    internal static class CoefficientScaling
    {
        internal static void Apply(int[] coefficients, int[] scaled, int size, int[,] matrix,
            int levelScale, int quantizationShift, int bitDepthShift, int minimum, int maximum)
        {
            int offset = 1 << (bitDepthShift - 1);
            if (matrix is null)
            {
                int factor = 16 * levelScale;
                int count = size * size;
                for (int index = 0; index < count; index++)
                {
                    scaled[index] = MathExtra.Clip3(minimum, maximum,
                        ((coefficients[index] * factor << quantizationShift) + offset) >> bitDepthShift);
                }
            }
            else
            {
                int index = 0;
                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        scaled[index] = MathExtra.Clip3(minimum, maximum,
                            ((coefficients[index] * matrix[x, y] * levelScale << quantizationShift) + offset) >> bitDepthShift);
                        index++;
                    }
                }
            }
        }
    }
}
