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
    internal static class ResidualDecoder
    {
        internal static void Decode(BitStreamWithNalSupport stream, slice_segment_header header,
            int x0, int y0, int log2TrafoSize, int cIdx)
        {
            pic_parameter_set_rbsp pps = header.pps;
            seq_parameter_set_rbsp sps = pps.sps;
            HeicPicture picture = header.parentPicture;
            DecoderContext context = stream.Context;
            Cabac cabac = stream.Cabac;

            context.transform_skip_flag[cIdx] = false;

            bool explicitRdpcm = false;

            if (header.pps.transform_skip_enabled_flag && !context.cu_transquant_bypass_flag &&
                (log2TrafoSize <= pps.Log2MaxTransformSkipSize))
                context.transform_skip_flag[cIdx] = cabac.read_transform_skip_flag(cIdx);

            if (picture.CuPredMode[x0, y0] == PredMode.MODE_INTER &&
                header.pps.sps.sps_range_ext.explicit_rdpcm_enabled_flag &&
                (context.transform_skip_flag[cIdx] || context.cu_transquant_bypass_flag))
            {
                explicitRdpcm = cabac.read_explicit_rdpcm_flag(cIdx);
                if (explicitRdpcm)
                {
                    cabac.read_explicit_rdpcm_dir_flag(cIdx);
                }
            }

            int last_sig_coeff_x_suffix = 0;
            int last_sig_coeff_y_suffix = 0;
            int last_sig_coeff_x_prefix = cabac.read_last_sig_coeff_prefix(log2TrafoSize, cIdx, CabacType.last_sig_coeff_x_prefix);
            int last_sig_coeff_y_prefix = cabac.read_last_sig_coeff_prefix(log2TrafoSize, cIdx, CabacType.last_sig_coeff_y_prefix);

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

            int lastSubBlock = Scans.GetScanPosition(log2TrafoSize - 2, scanIdx,
                LastSignificantCoeffX >> 2, LastSignificantCoeffY >> 2);
            int lastScanPos = Scans.GetScanPosition(2, scanIdx,
                LastSignificantCoeffX & 3, LastSignificantCoeffY & 3);

            bool[,] coded_sub_block_flag = picture.Workspace.CodedSubBlocks;
            Array.Clear(coded_sub_block_flag, 0, coded_sub_block_flag.Length);
            bool transformSkip = context.transform_skip_flag[cIdx];
            bool persistentRiceAdaptation = sps.sps_range_ext.persistent_rice_adaptation_enabled_flag;
            bool signDataHiding = pps.sign_data_hiding_enabled_flag;
            int transformSize = 1 << log2TrafoSize;
            int[] coefficients = picture.Workspace.Coefficients;
            Array.Clear(coefficients, 0, transformSize * transformSize);
            int xS, yS, xC, yC;

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

                if (!coded_sub_block_flag[xS, yS])
                {
                    firstSubblock = false;
                    lastSubblock_greater1Ctx = lastInvocation_greater1Ctx;
                    continue;
                }

                if (i == lastSubBlock)
                {
                    significantCoefficients |= 1 << lastScanPos;
                }

                for (int n = (i == lastSubBlock) ? lastScanPos - 1 : 15; n >= 0; n--)
                {
                    xC = (xS << 2) + coefficientScan[n][0];
                    yC = (yS << 2) + coefficientScan[n][1];

                    if (n > 0 || !inferSbDcSigCoeffFlag)
                    {
                        if (cabac.read_sig_coeff_flag(picture,
                            transformSkip, xC, yC,
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

                int firstSigScanPos = 16;
                int lastSigScanPos = -1;
                byte numGreater1Flag = 0;
                int lastGreater1ScanPos = -1;
                int greaterThanOne = 0;
                int greaterThanTwo = 0;

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
                    explicitRdpcm)
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
                    }
                }

                int coefficientSigns = 0;
                for (int n = 15; n >= 0; n--)
                {
                    if ((significantCoefficients & (1 << n)) != 0 &&
                        (!signDataHiding || !signHidden || (n != firstSigScanPos)))
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
                if (!persistentRiceAdaptation) initRiceValue = 0;

                int cLastAbsLevel = 0;
                int cLastRiceParam = initRiceValue;
                bool firstInvoke = true;

                for (int n = 15; n >= 0; n--)
                {
                    if ((significantCoefficients & (1 << n)) != 0)
                    {
                        xC = (xS << 2) + coefficientScan[n][0];
                        yC = (yS << 2) + coefficientScan[n][1];
                        int remainingLevel = 0;
                        int baseLevel = 1 +
                            (((greaterThanOne & (1 << n)) != 0) ? 1 : 0) +
                            (((greaterThanTwo & (1 << n)) != 0) ? 1 : 0);

                        if (baseLevel == ((numSigCoeff < 8) ?
                            ((n == lastGreater1ScanPos) ? 3 : 2) : 1))
                        {
                            remainingLevel = cabac.read_coeff_abs_level_remaining(cLastRiceParam);

                            // 9.3.3.11
                            cLastAbsLevel = baseLevel + remainingLevel;
                            cLastRiceParam += (cLastAbsLevel > (3 * (1 << cLastRiceParam)) ? 1 : 0);

                            if (!persistentRiceAdaptation)
                                cLastRiceParam = Math.Min(cLastRiceParam, 4);

                            if (persistentRiceAdaptation && firstInvoke)
                            {
                                if (remainingLevel >= (3 << (cabac.StatCoeff[sbType] / 4)))
                                {
                                    cabac.StatCoeff[sbType]++;
                                }
                                else if (2 * remainingLevel < (1 << (cabac.StatCoeff[sbType] / 4)) &&
                                    cabac.StatCoeff[sbType] > 0)
                                {
                                    cabac.StatCoeff[sbType]--;
                                }

                                firstInvoke = false;
                            }
                        }

                        TransCoeffLevel = (remainingLevel + baseLevel) * (((coefficientSigns & (1 << n)) != 0) ? -1 : 1);

                        if (signDataHiding && signHidden)
                        {
                            sumAbsLevel += (remainingLevel + baseLevel);
                            if ((n == firstSigScanPos) && ((sumAbsLevel % 2) == 1))
                                TransCoeffLevel = -1 * TransCoeffLevel;
                        }

                        coefficients[xC * transformSize + yC] = TransCoeffLevel;

                        numSigCoeff++;
                    }
                }
            }
        }
    }
}
