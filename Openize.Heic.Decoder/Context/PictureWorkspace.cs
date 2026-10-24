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
        internal readonly PointMap<int> SliceHeaderIndex;
        internal readonly BlockMap<IntraPredMode> IntraPredModeY;
        internal readonly BlockMap<IntraPredMode> IntraPredModeC;
        internal readonly BlockMap<int> CtDepth;
        internal readonly BlockMap<PredMode> CuPredMode;
        internal readonly BlockMap<int> QpY;
        internal readonly BlockMap<int> Log2CbSize;
        internal readonly bool[,] merge_flag;
        internal readonly PointMap<bool> cu_skip_flag;
        internal readonly PointMap<bool> palette_mode_flag;
        internal readonly BlockMap<bool> pcm_flag;
        internal readonly PointMap<bool> prev_intra_luma_pred_flag;
        internal readonly PointMap<int> mpm_idx;
        internal readonly PointMap<IntraPredMode> rem_intra_luma_pred_mode;
        internal readonly PointMap<byte> intra_chroma_pred_mode;
        internal readonly PointMap<bool> tu_residual_act_flag;
        internal readonly CodedBlockFlags cbf_cb;
        internal readonly CodedBlockFlags cbf_cr;
        internal readonly CodedBlockFlags cbf_luma;

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

            for (int i = 0; i < chroma_count; i++)
            {
                SaoTypeIdx[i] = new int[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY];
                SaoEoClass[i] = new int[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY];
                ResScaleVal[i] = new int[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY];
                SaoOffsetVal[i] = new int[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY][];
                sao_offset_abs[i] = new int[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY][];
                sao_offset_sign[i] = new int[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY][];
                sao_band_position[i] = new int[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY];
            }

            CtDepth = new BlockMap<int>(widthMax, heightMax);
            CuPredMode = new BlockMap<PredMode>(widthMax, heightMax);
            QpY = new BlockMap<int>(widthMax, heightMax);
            Log2CbSize = new BlockMap<int>(widthMax, heightMax);
            IntraPredModeY = new BlockMap<IntraPredMode>(widthMax, heightMax);
            IntraPredModeC = new BlockMap<IntraPredMode>(widthMax, heightMax);

            merge_flag = new bool[widthMax, heightMax];
            cu_skip_flag = new PointMap<bool>(widthMax, heightMax);
            palette_mode_flag = new PointMap<bool>(widthMax, heightMax);
            pcm_flag = new BlockMap<bool>(widthMax, heightMax);
            prev_intra_luma_pred_flag = new PointMap<bool>(widthMax, heightMax);

            mpm_idx = new PointMap<int>(widthMax, heightMax);
            rem_intra_luma_pred_mode = new PointMap<IntraPredMode>(widthMax, heightMax);
            intra_chroma_pred_mode = new PointMap<byte>(widthMax, heightMax);

            tu_residual_act_flag = new PointMap<bool>(widthMax, heightMax);

            cbf_cb = new CodedBlockFlags(widthMax, heightMax);
            cbf_cr = new CodedBlockFlags(widthMax, heightMax);
            cbf_luma = new CodedBlockFlags(widthMax, heightMax);

            SliceAddrRs = new uint[sps.PicWidthInCtbsY, sps.PicHeightInCtbsY];
            SliceHeaderIndex = new PointMap<int>(widthMax, heightMax);
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
            Array.Clear(SliceAddrRs, 0, SliceAddrRs.Length);
            SliceHeaderIndex.Clear();
            IntraPredModeY.Clear();
            IntraPredModeC.Clear();
            CtDepth.Clear();
            CuPredMode.Clear();
            QpY.Clear();
            Log2CbSize.Clear();
            Array.Clear(merge_flag, 0, merge_flag.Length);
            cu_skip_flag.Clear();
            palette_mode_flag.Clear();
            pcm_flag.Clear();
            prev_intra_luma_pred_flag.Clear();
            mpm_idx.Clear();
            rem_intra_luma_pred_mode.Clear();
            intra_chroma_pred_mode.Clear();
            tu_residual_act_flag.Clear();
            cbf_cb.Clear();
            cbf_cr.Clear();
            cbf_luma.Clear();
        }
    }
}
