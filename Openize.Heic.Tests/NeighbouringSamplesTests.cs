/*
 * Openize.HEIC
 * Copyright (c) 2024-2026 Openize Pty Ltd.
 *
 * This file is part of Openize.HEIC.
 *
 * Openize.HEIC is available under Openize license, which is
 * available along with Openize.HEIC sources.
 */

using Openize.Heic.Decoder;

namespace Openize.Heic.Tests
{
    public class NeighbouringSamplesTests
    {
        [Test]
        public void NeighbourAvailabilityDoesNotLeakBetweenBlockSizes()
        {
            NeighbouringSamplesAvailability available = new NeighbouringSamplesAvailability(32);
            foreach (int size in new[] { 32, 4, 16, 8, 32 })
            {
                available.Reset(size);
                Assert.That(available[-1, -1], Is.False);
                available[-1, -1] = true;
                for (int index = 0; index < 2 * size; index++)
                {
                    Assert.That(available[-1, index], Is.False);
                    Assert.That(available[index, -1], Is.False);
                    available[-1, index] = true;
                    available[index, -1] = true;
                }
            }
        }

    }
}
