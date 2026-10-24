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
    internal class NeighbouringSamplesAvailability : NeighbouringSamplesTemplate<bool>
    {
        public NeighbouringSamplesAvailability(int maximumSize) : base(maximumSize) { }

        public void Reset(int transformSize)
        {
            SetSize(transformSize);
            Array.Clear(data, 0, 4 * transformSize + 1);
        }
    }
}
