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
    internal class NeighbouringSamples : NeighbouringSamplesTemplate<ushort>
    {
        public NeighbouringSamples(int maximumSize) : base(maximumSize) { }

        public void Reset(int bitDepth)
        {
            ushort value = (ushort)(1 << (bitDepth - 1));
            int length = 4 * size + 1;
            for (int i = 0; i < length; i++)
            {
                data[i] = value;
            }
        }
    }
}
