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
    internal class BlockWorkspace
    {
        private const int MaximumTransformSize = 32;
        private const int CoefficientsPerSubBlock = 16;
        private const int MaximumSubBlocksPerDimension = MaximumTransformSize / 4;

        internal int[,] Predicted { get; } = new int[MaximumTransformSize, MaximumTransformSize];
        internal int[,] Scaled { get; } = new int[MaximumTransformSize, MaximumTransformSize];
        internal int[,] Intermediate { get; } = new int[MaximumTransformSize, MaximumTransformSize];
        internal int[,] Transformed { get; } = new int[MaximumTransformSize, MaximumTransformSize];
        internal int[] TransformInput { get; } = new int[MaximumTransformSize];
        internal int[] TransformOutput { get; } = new int[MaximumTransformSize];
        internal int[] Reference { get; } = new int[3 * MaximumTransformSize + 1];
        internal bool[,] CodedSubBlocks { get; } = new bool[MaximumSubBlocksPerDimension, MaximumSubBlocksPerDimension];
        internal int[] RemainingLevels { get; } = new int[CoefficientsPerSubBlock];
    }
}
