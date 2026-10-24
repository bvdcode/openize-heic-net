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
    internal class PictureWorkspace
    {
        internal BlockWorkspace Block { get; } = new BlockWorkspace();
        private readonly uint width;
        private readonly uint height;
        private readonly uint ctbWidth;
        private readonly uint ctbHeight;
        private readonly int components;

        internal readonly int[][,] SaoTypeIdx;
        internal readonly int[][,][] SaoOffsetVal;
        internal readonly int[][,] SaoEoClass;
        internal readonly int[][,] ResScaleVal;
        internal readonly int[][,][] sao_offset_abs;
        internal readonly int[][,][] sao_offset_sign;
        internal readonly int[][,] sao_band_position;
        internal readonly uint[,] SliceAddrRs;
        internal readonly int[,] SliceHeaderIndex;
        internal readonly IntraPredMode[,] IntraPredModeY;
        internal readonly IntraPredMode[,] IntraPredModeC;
        internal readonly int[][,] TransCoeffLevel;
        internal readonly int[,] CtDepth;
        internal readonly PredMode[,] CuPredMode;
        internal readonly int[,] QpY;
        internal readonly int[,] Log2CbSize;
        internal readonly bool[,] merge_flag;
        internal readonly bool[,] cu_skip_flag;
        internal readonly bool[,] palette_mode_flag;
        internal readonly bool[,] pcm_flag;
        internal readonly bool[,] prev_intra_luma_pred_flag;
        internal readonly int[,] mpm_idx;
        internal readonly IntraPredMode[,] rem_intra_luma_pred_mode;
        internal readonly byte[,] intra_chroma_pred_mode;
        internal readonly bool[,] tu_residual_act_flag;
        internal readonly bool[,][] cbf_cb;
        internal readonly bool[,][] cbf_cr;
        internal readonly bool[,][] cbf_luma;

        internal PictureWorkspace(seq_parameter_set_rbsp sps)
        {
            uint widthMax = sps.PicWidthInCtbsY << sps.CtbLog2SizeY;
            uint heightMax = sps.PicHeightInCtbsY << sps.CtbLog2SizeY;
            int chroma_count = sps.ChromaArrayType != 0 ? 3 : 1;
            width = widthMax;
            height = heightMax;
            ctbWidth = sps.PicWidthInCtbsY;
            ctbHeight = sps.PicHeightInCtbsY;
            components = chroma_count;

            SaoTypeIdx = new int[chroma_count][,];
            SaoEoClass = new int[chroma_count][,];
            ResScaleVal = new int[chroma_count][,];
            SaoOffsetVal = new int[chroma_count][,][];
            sao_offset_abs = new int[chroma_count][,][];
            sao_offset_sign = new int[chroma_count][,][];
            sao_band_position = new int[chroma_count][,];
            TransCoeffLevel = new int[chroma_count][,];

            for (int i = 0; i < chroma_count; i++)
            {
                SaoTypeIdx[i] = new int[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY];
                SaoEoClass[i] = new int[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY];
                ResScaleVal[i] = new int[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY];
                SaoOffsetVal[i] = new int[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY][];
                sao_offset_abs[i] = new int[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY][];
                sao_offset_sign[i] = new int[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY][];
                sao_band_position[i] = new int[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY];

                TransCoeffLevel[i] = new int[widthMax, heightMax];
            }

            CtDepth = new int[widthMax, heightMax];
            CuPredMode = new PredMode[widthMax, heightMax];
            QpY = new int[widthMax, heightMax];
            Log2CbSize = new int[widthMax, heightMax];
            IntraPredModeY = new IntraPredMode[widthMax, heightMax];
            IntraPredModeC = new IntraPredMode[widthMax, heightMax];

            merge_flag = new bool[widthMax, heightMax];
            cu_skip_flag = new bool[widthMax, heightMax];
            palette_mode_flag = new bool[widthMax, heightMax];
            pcm_flag = new bool[widthMax, heightMax];
            prev_intra_luma_pred_flag = new bool[widthMax, heightMax];

            mpm_idx = new int[widthMax, heightMax];
            rem_intra_luma_pred_mode = new IntraPredMode[widthMax, heightMax];
            intra_chroma_pred_mode = new byte[widthMax, heightMax];

            tu_residual_act_flag = new bool[widthMax, heightMax];

            cbf_cb = new bool[widthMax, heightMax][];
            cbf_cr = new bool[widthMax, heightMax][];
            cbf_luma = new bool[widthMax, heightMax][];

            SliceAddrRs = new uint[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY];
            SliceHeaderIndex = new int[widthMax, heightMax];
        }

        internal bool Matches(seq_parameter_set_rbsp sps)
        {
            return width == (sps.PicWidthInCtbsY << sps.CtbLog2SizeY) &&
                height == (sps.PicHeightInCtbsY << sps.CtbLog2SizeY) &&
                ctbWidth == sps.PicWidthInCtbsY && ctbHeight == sps.PicHeightInCtbsY &&
                components == (sps.ChromaArrayType != 0 ? 3 : 1);
        }

        internal void Clear()
        {
            foreach (Array plane in SaoTypeIdx)
            {
                Array.Clear(plane, 0, plane.Length);
            }
            foreach (Array plane in SaoOffsetVal)
            {
                Array.Clear(plane, 0, plane.Length);
            }
            foreach (Array plane in SaoEoClass)
            {
                Array.Clear(plane, 0, plane.Length);
            }
            foreach (Array plane in ResScaleVal)
            {
                Array.Clear(plane, 0, plane.Length);
            }
            foreach (Array plane in sao_offset_abs)
            {
                Array.Clear(plane, 0, plane.Length);
            }
            foreach (Array plane in sao_offset_sign)
            {
                Array.Clear(plane, 0, plane.Length);
            }
            foreach (Array plane in sao_band_position)
            {
                Array.Clear(plane, 0, plane.Length);
            }
            foreach (Array plane in TransCoeffLevel)
            {
                Array.Clear(plane, 0, plane.Length);
            }
            Array.Clear(SliceAddrRs, 0, SliceAddrRs.Length);
            Array.Clear(SliceHeaderIndex, 0, SliceHeaderIndex.Length);
            Array.Clear(IntraPredModeY, 0, IntraPredModeY.Length);
            Array.Clear(IntraPredModeC, 0, IntraPredModeC.Length);
            Array.Clear(CtDepth, 0, CtDepth.Length);
            Array.Clear(CuPredMode, 0, CuPredMode.Length);
            Array.Clear(QpY, 0, QpY.Length);
            Array.Clear(Log2CbSize, 0, Log2CbSize.Length);
            Array.Clear(merge_flag, 0, merge_flag.Length);
            Array.Clear(cu_skip_flag, 0, cu_skip_flag.Length);
            Array.Clear(palette_mode_flag, 0, palette_mode_flag.Length);
            Array.Clear(pcm_flag, 0, pcm_flag.Length);
            Array.Clear(prev_intra_luma_pred_flag, 0, prev_intra_luma_pred_flag.Length);
            Array.Clear(mpm_idx, 0, mpm_idx.Length);
            Array.Clear(rem_intra_luma_pred_mode, 0, rem_intra_luma_pred_mode.Length);
            Array.Clear(intra_chroma_pred_mode, 0, intra_chroma_pred_mode.Length);
            Array.Clear(tu_residual_act_flag, 0, tu_residual_act_flag.Length);
            Array.Clear(cbf_cb, 0, cbf_cb.Length);
            Array.Clear(cbf_cr, 0, cbf_cr.Length);
            Array.Clear(cbf_luma, 0, cbf_luma.Length);
        }
    }
}
