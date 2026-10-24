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
using Openize.Heic.Decoder.IO;

namespace Openize.Heic.Decoder
{
    // 7.4.9.11 Residual coding semantics
    internal class residual_coding
    {
        internal int last_sig_coeff_x_prefix;
        internal int last_sig_coeff_y_prefix;
        internal int last_sig_coeff_x_suffix;
        internal int last_sig_coeff_y_suffix;

        private bool[,] coded_sub_block_flag;

        public residual_coding(BitStreamWithNalSupport stream, slice_segment_header header,
            int x0, int y0, int log2TrafoSize, int cIdx)
        {
            var pps = header.pps;
            var sps = header.pps.sps;
            var picture = header.parentPicture;
            DecoderContext context = stream.Context;
            Cabac cabac = stream.Cabac;

            context.transform_skip_flag[cIdx] = false;

            if (picture.explicit_rdpcm_flag[x0, y0] == null)
                picture.explicit_rdpcm_flag[x0, y0] = new bool[3];

            if (header.pps.transform_skip_enabled_flag && !context.cu_transquant_bypass_flag &&
                (log2TrafoSize <= pps.Log2MaxTransformSkipSize))
                context.transform_skip_flag[cIdx] = cabac.read_transform_skip_flag(cIdx);

            if (picture.CuPredMode[x0, y0] == PredMode.MODE_INTER &&
                header.pps.sps.sps_range_ext.explicit_rdpcm_enabled_flag &&
                (context.transform_skip_flag[cIdx] || context.cu_transquant_bypass_flag))
            {
                picture.explicit_rdpcm_flag[x0, y0][cIdx] = cabac.read_explicit_rdpcm_flag(cIdx);

                if (picture.explicit_rdpcm_flag[x0, y0][cIdx])
                    picture.explicit_rdpcm_dir_flag[x0, y0][cIdx] = cabac.read_explicit_rdpcm_dir_flag(cIdx);
            }

            last_sig_coeff_x_prefix = cabac.read_last_sig_coeff_prefix(log2TrafoSize, cIdx, CabacType.last_sig_coeff_x_prefix);
            last_sig_coeff_y_prefix = cabac.read_last_sig_coeff_prefix(log2TrafoSize, cIdx, CabacType.last_sig_coeff_y_prefix);

            if (last_sig_coeff_x_prefix > 3)
                last_sig_coeff_x_suffix = cabac.read_last_sig_coeff_suffix(last_sig_coeff_x_prefix);

            if (last_sig_coeff_y_prefix > 3)
                last_sig_coeff_y_suffix = cabac.read_last_sig_coeff_suffix(last_sig_coeff_y_prefix);

            int LastSignificantCoeffX =
                (last_sig_coeff_x_prefix <= 3) ? last_sig_coeff_x_prefix :
                (1 << ((last_sig_coeff_x_prefix >> 1) - 1)) * (2 + (last_sig_coeff_x_prefix & 1)) + last_sig_coeff_x_suffix;

            int LastSignificantCoeffY =
                (last_sig_coeff_y_prefix <= 3) ? last_sig_coeff_y_prefix :
                (1 << ((last_sig_coeff_y_prefix >> 1) - 1)) * (2 + (last_sig_coeff_y_prefix & 1)) + last_sig_coeff_y_suffix;

            // define scanIdx
            int scanIdx = 0;
            if (picture.CuPredMode[x0, y0] == PredMode.MODE_INTRA)
            {
                if ((log2TrafoSize == 2) ||
                    (log2TrafoSize == 3 && cIdx == 0) ||
                    (log2TrafoSize == 3 && header.pps.sps.ChromaArrayType == 3))
                {
                    IntraPredMode predModeIntra = cIdx == 0 ? picture.IntraPredModeY[x0, y0] : picture.IntraPredModeC[x0, y0];

                    if ((int)predModeIntra >= 6 && (int)predModeIntra <= 14)
                        scanIdx = 2;

                    if ((int)predModeIntra >= 22 && (int)predModeIntra <= 30)
                        scanIdx = 1;
                }
            }

            if (scanIdx == 2)
                (LastSignificantCoeffX, LastSignificantCoeffY) = (LastSignificantCoeffY, LastSignificantCoeffX);

            byte[][] subBlockScan = Scans.ScanOrder[log2TrafoSize - 2][scanIdx];
            byte[][] coefficientScan = Scans.ScanOrder[2][scanIdx];

            int lastScanPos = 16;
            int lastSubBlock = (1 << (log2TrafoSize - 2)) * (1 << (log2TrafoSize - 2)) - 1;

            uint xS, yS, xC, yC;
            do
            {
                if (lastScanPos == 0)
                {
                    lastScanPos = 16;
                    lastSubBlock--;
                }

                lastScanPos--;

                xS = subBlockScan[lastSubBlock][0];
                yS = subBlockScan[lastSubBlock][1];

                xC = (xS << 2) + coefficientScan[lastScanPos][0];
                yC = (yS << 2) + coefficientScan[lastScanPos][1];
            } while ((xC != LastSignificantCoeffX) || (yC != LastSignificantCoeffY));

            coded_sub_block_flag = picture.Workspace.CodedSubBlocks;
            Array.Clear(coded_sub_block_flag, 0, coded_sub_block_flag.Length);

            int lastSubblock_greater1Ctx = 0;

            int lastInvocation_ctxSet = 0;
            int lastInvocation_greater1Ctx = 0;
            bool lastInvocation_coeff_abs_level_greater1_flag = false;
            bool firstSubblock = true;
            bool firstCoeffInSubblock;

            for (int i = lastSubBlock; i >= 0; i--)
            {
                xS = subBlockScan[i][0];
                yS = subBlockScan[i][1];

                int significantCoefficients = 0;
                int escapeDataPresent = 0;
                bool inferSbDcSigCoeffFlag = false;

                if ((i < lastSubBlock) && (i > 0))
                {
                    coded_sub_block_flag[xS, yS] = cabac.read_coded_sub_block_flag(
                        xS, yS, coded_sub_block_flag, cIdx, log2TrafoSize);
                    inferSbDcSigCoeffFlag = true;
                }
                else if (i == 0 || i == lastSubBlock)
                {
                    coded_sub_block_flag[xS, yS] = true;
                }

                if (i == lastSubBlock)
                {
                    significantCoefficients |= 1 << lastScanPos;
                }

                for (int n = (i == lastSubBlock) ? lastScanPos - 1 : 15; n >= 0; n--)
                {
                    xC = (xS << 2) + coefficientScan[n][0];
                    yC = (yS << 2) + coefficientScan[n][1];

                    if (coded_sub_block_flag[xS, yS])
                    {
                        if (n > 0 || !inferSbDcSigCoeffFlag)
                        {
                            if (cabac.read_sig_coeff_flag(picture,
                                context.transform_skip_flag[cIdx], (int)xC, (int)yC,
                                coded_sub_block_flag, cIdx, log2TrafoSize, scanIdx))
                            {
                                significantCoefficients |= 1 << n;
                                inferSbDcSigCoeffFlag = false;
                            }
                        }
                        else
                        {
                            significantCoefficients |= 1 << n;
                        }
                    }

                }

                int firstSigScanPos = 16;
                int lastSigScanPos = -1;
                byte numGreater1Flag = 0;
                int lastGreater1ScanPos = -1;
                int greaterThanOne = 0;
                int greaterThanTwo = 0;
                int[] coeff_abs_level_remaining = picture.Workspace.RemainingLevels;
                Array.Clear(coeff_abs_level_remaining, 0, coeff_abs_level_remaining.Length);

                firstCoeffInSubblock = true;

                for (int n = 15; n >= 0; n--)
                {
                    if ((significantCoefficients & (1 << n)) != 0)
                    {
                        if (numGreater1Flag < 8)
                        {
                            bool greater1 = cabac.read_coeff_abs_level_greater1_flag(cIdx, i,
                                firstCoeffInSubblock, firstSubblock,
                                lastSubblock_greater1Ctx,
                                ref lastInvocation_ctxSet,
                                ref lastInvocation_greater1Ctx,
                                ref lastInvocation_coeff_abs_level_greater1_flag);
                            if (greater1)
                            {
                                greaterThanOne |= 1 << n;
                            }

                            numGreater1Flag++;

                            if (((greaterThanOne & (1 << n)) != 0) && lastGreater1ScanPos == -1)
                                lastGreater1ScanPos = n;
                            else if (((greaterThanOne & (1 << n)) != 0))
                                escapeDataPresent = 1;
                        }
                        else
                        {
                            escapeDataPresent = 1;
                        }

                        if (lastSigScanPos == -1)
                            lastSigScanPos = n;

                        firstSigScanPos = n;
                        firstCoeffInSubblock = false;
                    }
                }
                firstSubblock = false;
                lastSubblock_greater1Ctx = lastInvocation_greater1Ctx;


                bool signHidden;
                IntraPredMode predModeIntra = cIdx == 0 ? picture.IntraPredModeY[x0, y0] : picture.IntraPredModeC[x0, y0];

                if (context.cu_transquant_bypass_flag ||
                    (picture.CuPredMode[x0, y0] == PredMode.MODE_INTRA &&
                    sps.sps_range_ext.implicit_rdpcm_enabled_flag &&
                    context.transform_skip_flag[cIdx] &&
                    ((int)predModeIntra == 10 || (int)predModeIntra == 26)) ||
                    picture.explicit_rdpcm_flag[x0, y0][cIdx])
                {
                    signHidden = false;
                }
                else
                {
                    signHidden = (lastSigScanPos - firstSigScanPos) > 3;
                }

                if (lastGreater1ScanPos != -1)
                {
                    if (cabac.read_coeff_abs_level_greater2_flag(cIdx, lastInvocation_ctxSet))
                    {
                        greaterThanTwo |= 1 << lastGreater1ScanPos;
                        escapeDataPresent = 1;
                    }
                }

                int coefficientSigns = 0;
                for (int n = 15; n >= 0; n--)
                {
                    if ((significantCoefficients & (1 << n)) != 0 &&
                        (!header.pps.sign_data_hiding_enabled_flag || !signHidden || (n != firstSigScanPos)))
                    {
                        if (cabac.read_coeff_sign_flag())
                        {
                            coefficientSigns |= 1 << n;
                        }
                    }
                }

                int numSigCoeff = 0;
                int sumAbsLevel = 0;
                int TransCoeffLevel;

                int sbType = (cIdx == 0) ? 2 : 0;
                if (context.transform_skip_flag[cIdx] || context.cu_transquant_bypass_flag)
                    sbType++;

                int initRiceValue = cabac.StatCoeff[sbType] / 4;
                if (!sps.sps_range_ext.persistent_rice_adaptation_enabled_flag) initRiceValue = 0;

                int cLastAbsLevel = 0;
                int cLastRiceParam = initRiceValue;
                bool firstInvoke = true;

                for (int n = 15; n >= 0; n--)
                {
                    xC = (xS << 2) + coefficientScan[n][0];
                    yC = (yS << 2) + coefficientScan[n][1];

                    if ((significantCoefficients & (1 << n)) != 0)
                    {
                        int baseLevel = 1 +
                            (((greaterThanOne & (1 << n)) != 0) ? 1 : 0) +
                            (((greaterThanTwo & (1 << n)) != 0) ? 1 : 0);

                        if (baseLevel == ((numSigCoeff < 8) ?
                            ((n == lastGreater1ScanPos) ? 3 : 2) : 1))
                        {
                            coeff_abs_level_remaining[n] = cabac.read_coeff_abs_level_remaining(cLastRiceParam);

                            // 9.3.3.11
                            cLastAbsLevel = baseLevel + coeff_abs_level_remaining[n];
                            cLastRiceParam += (cLastAbsLevel > (3 * (1 << cLastRiceParam)) ? 1 : 0);

                            if (!sps.sps_range_ext.persistent_rice_adaptation_enabled_flag)
                                cLastRiceParam = Math.Min(cLastRiceParam, 4);

                            if (sps.sps_range_ext.persistent_rice_adaptation_enabled_flag && firstInvoke)
                            {
                                if (coeff_abs_level_remaining[n] >= (3 << (cabac.StatCoeff[sbType] / 4)))
                                {
                                    cabac.StatCoeff[sbType]++;
                                }
                                else if (2 * coeff_abs_level_remaining[n] < (1 << (cabac.StatCoeff[sbType] / 4)) &&
                                    cabac.StatCoeff[sbType] > 0)
                                {
                                    cabac.StatCoeff[sbType]--;
                                }

                                firstInvoke = false;
                            }
                        }

                        TransCoeffLevel = (coeff_abs_level_remaining[n] + baseLevel) * (((coefficientSigns & (1 << n)) != 0) ? -1 : 1);

                        if (header.pps.sign_data_hiding_enabled_flag && signHidden)
                        {
                            sumAbsLevel += (coeff_abs_level_remaining[n] + baseLevel);
                            if ((n == firstSigScanPos) && ((sumAbsLevel % 2) == 1))
                                TransCoeffLevel = -1 * TransCoeffLevel;
                        }

                        if (cIdx == 0)
                            picture.TransCoeffLevel[cIdx][x0 + xC, y0 + yC] = TransCoeffLevel;
                        else
                            picture.TransCoeffLevel[cIdx][x0 / sps.SubWidthC + xC, y0 / sps.SubHeightC + yC] = TransCoeffLevel;

                        numSigCoeff++;
                    }
                }
            }
        }
    }
}
