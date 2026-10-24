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
    public class CoefficientScalingTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ScalingMatchesPerCoefficientReferenceIncludingIntegerOverflow(bool customMatrix)
        {
            Random random = new Random(638291);
            int[] coefficients = new int[32 * 32];
            int[] scaled = new int[32 * 32];
            int[] levels = { 40, 45, 51, 57, 64, 72 };
            for (int iteration = 0; iteration < 1000; iteration++)
            {
                int size = 1 << random.Next(2, 6);
                int bitDepth = random.Next(8, 17);
                int transformRange = random.Next(15, 23);
                int shift = bitDepth + (int)Math.Log2(size) + 10 - transformRange;
                int quantizationParameter = random.Next(0, 100);
                int minimum = -(1 << transformRange);
                int maximum = (1 << transformRange) - 1;
                int[,]? matrix = customMatrix ? new int[size, size] : null;
                Array.Fill(scaled, 1234567);
                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        coefficients[x * size + y] = random.Next(-1000000, 1000001);
                        if (matrix is not null)
                        {
                            matrix[x, y] = random.Next(1, 256);
                        }
                    }
                }
                CoefficientScaling.Apply(coefficients, scaled, size, matrix,
                    levels[quantizationParameter % 6], quantizationParameter / 6, shift, minimum, maximum);
                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        int scale = matrix is null ? 16 : matrix[x, y];
                        int expected = MathExtra.Clip3(minimum, maximum,
                            ((coefficients[x * size + y] * scale * levels[quantizationParameter % 6] <<
                            (quantizationParameter / 6)) + (1 << (shift - 1))) >> shift);
                        Assert.That(scaled[x * size + y], Is.EqualTo(expected));
                    }
                }
                for (int index = size * size; index < scaled.Length; index++)
                {
                    Assert.That(scaled[index], Is.EqualTo(1234567));
                }
            }
        }
    }
}
