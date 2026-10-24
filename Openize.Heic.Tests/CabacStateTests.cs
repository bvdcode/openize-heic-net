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
using Openize.Heic.Decoder.IO;

namespace Openize.Heic.Tests
{
    public class CabacStateTests
    {
        [Test]
        public void PackedStatesMatchSeparateFieldsAtEveryProbabilityAndRange()
        {
            byte[] bits = { 0x37, 0xA9, 0x00, 0x03, 0xF1, 0x64, 0x8B, 0x5E };
            for (int probability = 0; probability < 64; probability++)
            {
                for (int mostProbable = 0; mostProbable <= 1; mostProbable++)
                {
                    for (int range = 256; range < 512; range++)
                    {
                        int lps = Cabac.rangeTabLps[probability, (range >> 6) & 3];
                        int boundary = range - lps;
                        int[] offsets = { 0, boundary - 1, boundary, range - 1 };
                        foreach (int offset in offsets)
                        {
                            using BitStreamWithNalSupport referenceReader = new BitStreamWithNalSupport(new MemoryStream(bits, false));
                            using BitStreamWithNalSupport reader = new BitStreamWithNalSupport(new MemoryStream(bits, false));
                            Cabac cabac = reader.Cabac;
                            cabac.ivlCurrRange = range;
                            cabac.ivlOffset = offset;
                            byte model = (byte)((probability << 1) | mostProbable);
                            int expectedRange = boundary;
                            int expectedOffset = offset;
                            int expectedProbability;
                            bool expectedMostProbable = mostProbable != 0;
                            bool expectedBit;
                            if (expectedOffset >= expectedRange)
                            {
                                expectedBit = !expectedMostProbable;
                                expectedOffset -= expectedRange;
                                expectedRange = lps;
                                if (probability == 0)
                                {
                                    expectedMostProbable = !expectedMostProbable;
                                }
                                expectedProbability = Cabac.transIdxLps[probability];
                            }
                            else
                            {
                                expectedBit = expectedMostProbable;
                                expectedProbability = Cabac.transIdxMps[probability];
                            }
                            while (expectedRange < 256)
                            {
                                expectedRange <<= 1;
                                expectedOffset = (expectedOffset << 1) | referenceReader.ReadBit();
                            }
                            int actualBit = cabac.DecodeGeneralForBinaryDecision(ref model);
                            Assert.That(actualBit, Is.EqualTo(expectedBit ? 1 : 0));
                            Assert.That(model >> 1, Is.EqualTo(expectedProbability));
                            Assert.That((model & 1) != 0, Is.EqualTo(expectedMostProbable));
                            Assert.That(cabac.ivlCurrRange, Is.EqualTo(expectedRange));
                            Assert.That(cabac.ivlOffset, Is.EqualTo(expectedOffset));
                            Assert.That(reader.GetBitPosition(), Is.EqualTo(referenceReader.GetBitPosition()));
                        }
                    }
                }
            }
        }

        [Test]
        public void RestoringContextsDoesNotAliasTheSavedSnapshot()
        {
            using BitStreamWithNalSupport reader = new BitStreamWithNalSupport(new MemoryStream(new byte[8], false));
            Cabac cabac = reader.Cabac;
            for (int table = 0; table < cabac.contextTable.Length; table++)
            {
                cabac.contextTable[table] = new byte[table + 1];
                for (int context = 0; context <= table; context++)
                {
                    cabac.contextTable[table][context] = (byte)((table + context) & 127);
                }
            }
            cabac.StatCoeff[0] = 7;
            cabac.StatCoeff[3] = 11;
            cabac.SyncTables();
            for (int repetition = 0; repetition < 2; repetition++)
            {
                foreach (byte[] table in cabac.contextTable)
                {
                    Array.Fill(table, (byte)127);
                }
                Array.Fill(cabac.StatCoeff, (byte)255);
                cabac.RestoreSyncedTables();
                for (int table = 0; table < cabac.contextTable.Length; table++)
                {
                    for (int context = 0; context <= table; context++)
                    {
                        Assert.That(cabac.contextTable[table][context], Is.EqualTo((table + context) & 127));
                    }
                }
                Assert.That(cabac.StatCoeff, Is.EqualTo(new byte[] { 7, 0, 0, 11 }));
            }
        }
    }
}
