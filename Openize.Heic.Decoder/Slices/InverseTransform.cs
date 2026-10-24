/*
 * Openize.HEIC 
 * Copyright (c) 2024-2026 Openize Pty Ltd. 
 *
 * This file is part of Openize.HEIC.
 *
 * Openize.HEIC is available under Openize license, which is
 * available along with Openize.HEIC sources.
 */

namespace Openize.Heic.Decoder
{
    internal static class InverseTransform
    {
        internal static void Apply(int[] input, int[] output, int size, int[,] matrix, bool sine)
        {
            if (sine)
            {
                for (int i = 0; i < size; i++)
                {
                    int sum = 0;
                    for (int j = 0; j < size; j++)
                    {
                        sum += matrix[j, i] * input[j];
                    }
                    output[i] = sum;
                }
                return;
            }

            ApplyCosine(input, output, size, 1, matrix);
        }

        private static void ApplyCosine(int[] input, int[] output, int size, int inputStride, int[,] matrix)
        {
            bool dcOnly = true;
            for (int j = 1; j < size; j++)
            {
                if (input[j * inputStride] != 0)
                {
                    dcOnly = false;
                    break;
                }
            }

            if (dcOnly)
            {
                int dc = matrix[0, 0] * input[0];
                for (int i = 0; i < size; i++)
                {
                    output[i] = dc;
                }
                return;
            }

            int half = size / 2;
            ApplyCosine(input, output, half, inputStride * 2, matrix);
            int matrixStride = matrix.GetLength(0) / size;

            for (int i = 0; i < half; i++)
            {
                int odd = 0;
                for (int j = 1; j < size; j += 2)
                {
                    odd += matrix[j * matrixStride, i] * input[j * inputStride];
                }
                int even = output[i];
                output[i] = even + odd;
                output[size - i - 1] = even - odd;
            }
        }
    }
}
