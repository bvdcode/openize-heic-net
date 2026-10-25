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
    internal class BlockWorkspace
    {
        private const int MaximumTransformSize = 32;
        private const int SubBlockSize = 4;
        private const int MaximumSubBlocksPerDimension = MaximumTransformSize / SubBlockSize;

        internal NeighbouringSamples Neighbours { get; } = new NeighbouringSamples(MaximumTransformSize);
        internal NeighbouringSamples FilteredNeighbours { get; } = new NeighbouringSamples(MaximumTransformSize);
        internal NeighbouringSamplesAvailability NeighbourAvailability { get; } = new NeighbouringSamplesAvailability(MaximumTransformSize);
        internal IntraPredMode[] IntraPredModeCandidates { get; } = new IntraPredMode[3];
        internal int[,] Predicted { get; } = new int[MaximumTransformSize, MaximumTransformSize];
        internal int[] SignificantPositions { get; } = new int[SubBlockSize * SubBlockSize];
        internal int[] Coefficients { get; } = new int[MaximumTransformSize * MaximumTransformSize];
        internal int[] Scaled { get; } = new int[MaximumTransformSize * MaximumTransformSize];
        internal int[] Intermediate { get; } = new int[MaximumTransformSize * MaximumTransformSize];
        internal int[,] Transformed { get; } = new int[MaximumTransformSize, MaximumTransformSize];
        internal int[] TransformInput { get; } = new int[MaximumTransformSize];
        internal int[] TransformOutput { get; } = new int[MaximumTransformSize];
        internal int[] Reference { get; } = new int[3 * MaximumTransformSize + 1];
        internal bool[,] CodedSubBlocks { get; } = new bool[MaximumSubBlocksPerDimension, MaximumSubBlocksPerDimension];
    }
}
