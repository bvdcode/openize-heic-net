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
    public class InverseTransformTests
    {
        [TestCase(4, false)]
        [TestCase(8, false)]
        [TestCase(16, false)]
        [TestCase(32, false)]
        [TestCase(4, true)]
        public void TransformMatchesMatrixForSparseAndFullRangeInputs(int size, bool sine)
        {
            Random random = new Random(12345);
            int[,] matrix = sine ? TransformReferenceCoefficients.Sine4 : TransformReferenceCoefficients.Cosine32;
            int stride = sine ? 1 : 32 / size;
            int[] input = new int[size];
            int[] output = new int[size];
            for (int sample = 0; sample < 2000; sample++)
            {
                for (int j = 0; j < size; j++)
                {
                    input[j] = sample switch
                    {
                        0 => 0,
                        1 => j == 0 ? 32767 : 0,
                        2 => j == size - 1 ? -32768 : 0,
                        3 => int.MaxValue,
                        4 => int.MinValue,
                        _ => sample % 2 == 0 && (j % 3 != 0 || j >= sample % (size + 1)) ? 0 :
                            (int)random.NextInt64(int.MinValue, (long)int.MaxValue + 1)
                    };
                }
                InverseTransform.Apply(input, output, size, sine);
                for (int i = 0; i < size; i++)
                {
                    int expected = 0;
                    for (int j = 0; j < size; j++)
                    {
                        expected = unchecked(expected + matrix[j * stride, i] * input[j]);
                    }
                    Assert.That(output[i], Is.EqualTo(expected), $"Sample {sample}, index {i}");
                }
            }
        }
    }
}
