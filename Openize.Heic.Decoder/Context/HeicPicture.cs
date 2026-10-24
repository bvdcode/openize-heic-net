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
using System.Collections.Generic;

namespace Openize.Heic.Decoder
{
    internal class HeicPicture
    {
        internal BlockWorkspace Workspace { get; }

        internal Scaling Scaling { get; }

        public Dictionary<int, slice_segment_header> SliceHeaders { get; }
        public Dictionary<int, slice_segment_data> SliceUnits { get; }

        public video_parameter_set_rbsp vps;
        public seq_parameter_set_rbsp sps;
        public pic_parameter_set_rbsp pps;
        public DecoderContext Context;
        public NalHeader NalHeader;

        public int PicOrderCntVal;
        public int picture_order_cnt_lsb;

        public int[][,] SaoTypeIdx;
        public int[][,][] SaoOffsetVal;
        public int[][,] SaoEoClass;

        public int[][,] ResScaleVal;

        public int[][,][] sao_offset_abs;
        public int[][,][] sao_offset_sign;
        public int[][,] sao_band_position;

        public uint[,] SliceAddrRs;
        public int[,] SliceHeaderIndex;
        public BlockMap<IntraPredMode> IntraPredModeY;
        public BlockMap<IntraPredMode> IntraPredModeC;


        public BlockMap<int> CtDepth;
        public BlockMap<PredMode> CuPredMode;
        public BlockMap<int> QpY;
        public BlockMap<int> Log2CbSize;
        public bool[,] merge_flag;

        public bool[,] cu_skip_flag;
        public bool[,] palette_mode_flag;
        public bool[,] pcm_flag;
        public bool[,] prev_intra_luma_pred_flag;
        public int[,] mpm_idx;
        public IntraPredMode[,] rem_intra_luma_pred_mode;
        public byte[,] intra_chroma_pred_mode;

        public bool[,] tu_residual_act_flag;

        //public bool[,][] split_transform_flag;
        public CodedBlockFlags cbf_cb;
        public CodedBlockFlags cbf_cr;
        public CodedBlockFlags cbf_luma;

        //public bool[,][] transform_skip_flag;

        public int[] pcm_sample_luma;
        public int[] pcm_sample_chroma;

        public byte[][,] pixels;
        public ushort[][,] pixels_high_color_range;

        public HeicPicture(slice_segment_header slice_header, NalHeader nal_header, TileDecoderWorkspace workspace)
        {
            SliceHeaders = new Dictionary<int, slice_segment_header>();
            SliceUnits = new Dictionary<int, slice_segment_data>();

            pps = slice_header.pps;
            sps = pps.sps;
            vps = sps.vps;
            Scaling = new Scaling(sps);
            NalHeader = nal_header;

            PictureWorkspace buffers;
            if (workspace is null)
            {
                buffers = new PictureWorkspace(sps);
            }
            else
            {
                buffers = workspace.GetPictureWorkspace(sps);
            }
            Workspace = buffers.Block;
            SaoTypeIdx = buffers.SaoTypeIdx;
            SaoOffsetVal = buffers.SaoOffsetVal;
            SaoEoClass = buffers.SaoEoClass;
            ResScaleVal = buffers.ResScaleVal;
            sao_offset_abs = buffers.sao_offset_abs;
            sao_offset_sign = buffers.sao_offset_sign;
            sao_band_position = buffers.sao_band_position;
            SliceAddrRs = buffers.SliceAddrRs;
            SliceHeaderIndex = buffers.SliceHeaderIndex;
            IntraPredModeY = buffers.IntraPredModeY;
            IntraPredModeC = buffers.IntraPredModeC;
            CtDepth = buffers.CtDepth;
            CuPredMode = buffers.CuPredMode;
            QpY = buffers.QpY;
            Log2CbSize = buffers.Log2CbSize;
            merge_flag = buffers.merge_flag;
            cu_skip_flag = buffers.cu_skip_flag;
            palette_mode_flag = buffers.palette_mode_flag;
            pcm_flag = buffers.pcm_flag;
            prev_intra_luma_pred_flag = buffers.prev_intra_luma_pred_flag;
            mpm_idx = buffers.mpm_idx;
            rem_intra_luma_pred_mode = buffers.rem_intra_luma_pred_mode;
            intra_chroma_pred_mode = buffers.intra_chroma_pred_mode;
            tu_residual_act_flag = buffers.tu_residual_act_flag;
            cbf_cb = buffers.cbf_cb;
            cbf_cr = buffers.cbf_cr;
            cbf_luma = buffers.cbf_luma;

            uint widthMax = sps.PicWidthInCtbsY << sps.CtbLog2SizeY;
            uint heightMax = sps.PicHeightInCtbsY << sps.CtbLog2SizeY;
            int chroma_count = sps.ChromaArrayType != 0 ? 3 : 1;
            bool highColorRange = sps.BitDepthY > 8 || sps.BitDepthC > 8;
            if (highColorRange)
            {
                pixels_high_color_range = new ushort[chroma_count][,];
            }
            else
            {
                pixels = new byte[chroma_count][,];
            }
            for (int component = 0; component < chroma_count; component++)
            {
                uint componentWidth = widthMax;
                uint componentHeight = heightMax;
                if (component > 0)
                {
                    componentWidth /= sps.SubWidthC;
                    componentHeight /= sps.SubHeightC;
                }

                if (highColorRange)
                {
                    pixels_high_color_range[component] = new ushort[componentWidth, componentHeight];
                }
                else
                {
                    pixels[component] = new byte[componentWidth, componentHeight];
                }
            }
        }

        public void CleanMemory()
        {
            SaoTypeIdx = null;
            SaoOffsetVal = null;
            SaoEoClass = null;
            ResScaleVal = null;
            sao_offset_abs = null;
            sao_offset_sign = null;
            sao_band_position = null;
            SliceAddrRs = null;
            SliceHeaderIndex = null;
            IntraPredModeY = null;
            IntraPredModeC = null;
            CtDepth = null;
            CuPredMode = null;
            QpY = null;
            Log2CbSize = null;
            merge_flag = null;
            cu_skip_flag = null;
            palette_mode_flag = null;
            pcm_flag = null;
            prev_intra_luma_pred_flag = null;
            mpm_idx = null;
            rem_intra_luma_pred_mode = null;
            intra_chroma_pred_mode = null;
            tu_residual_act_flag = null;
            cbf_cb = null;
            cbf_cr = null;
            cbf_luma = null;
            pcm_sample_luma = null;
            pcm_sample_chroma = null;
        }

        // 6.4.1 Derivation process for z-scan order block availability
        // 
        // The luma location ( xCurr, yCurr ) of the top-left sample of the current block
        // relative to the top-left luma sample of the current picture
        //
        // The luma location ( xNbY, yNbY ) covered by a neighbouring block relative
        // to the top-left luma sample of the current picture.
        internal bool CheckZScanAvaliability(int xCurr, int yCurr, int xNbY, int yNbY)
        {
            if (xNbY < 0 || yNbY < 0)
                return false;

            if (xNbY >= sps.pic_width_in_luma_samples || yNbY >= sps.pic_height_in_luma_samples)
                return false;

            // The minimum luma block address in z-scan order minBlockAddrCurr of the current block
            uint minBlockAddrCurr = pps.MinTbAddrZs[xCurr >> sps.MinTbLog2SizeY, yCurr >> sps.MinTbLog2SizeY];

            // The minimum luma block address in z-scan order minBlockAddrN of the neighbouring block covering the location (xNbY, yNbY)
            uint minBlockAddrN = pps.MinTbAddrZs[xNbY >> sps.MinTbLog2SizeY, yNbY >> sps.MinTbLog2SizeY];

            if (minBlockAddrN > minBlockAddrCurr)
                return false;

            // SliceAddrRs associated with the slice segment containing the current block
            int xCurrCtb = xCurr >> sps.CtbLog2SizeY;
            int yCurrCtb = yCurr >> sps.CtbLog2SizeY;
            int ctbCurrAddrRs = yCurrCtb * (int)sps.PicWidthInCtbsY + xCurrCtb;

            // SliceAddrRs associated with the slice segment containing the neighbouring block
            int xNbCtb = xNbY >> sps.CtbLog2SizeY;
            int yNbCtb = yNbY >> sps.CtbLog2SizeY;
            int ctbNbAddrRs = yNbCtb * (int)sps.PicWidthInCtbsY + xNbCtb;

            // the current block and the neighbouring block are in the same slice
            if (SliceAddrRs[xCurrCtb, yCurrCtb] != SliceAddrRs[xNbCtb, yNbCtb])
                return false;

            // the neighbouring block is contained in a different tile than the current block
            if (pps.TileIdFromRs[ctbCurrAddrRs] != pps.TileIdFromRs[ctbNbAddrRs])
                return false;

            return true;
        }

        // 6.4.2 Derivation process for prediction block availability
        //
        // the luma location ( xCb, yCb ) of the top-left sample of the current luma coding block
        // relative to the top-left luma sample of the current picture
        //
        // a variable nCbS specifying the size of the current luma coding block
        //
        // the luma location ( xPb, yPb ) of the top-left sample of the current luma prediction block
        // relative to the top-left luma sample of the current picture
        //
        // two variables nPbW and nPbH specifying the width and the height of the current luma prediction block
        //
        // a variable partIdx specifying the partition index of the current prediction unit within the current coding unit
        //
        // the luma location ( xNbY, yNbY ) covered by a neighbouring prediction block
        // relative to the top-left luma sample of the current picture
        //
        // Output of this process is the availability of the neighbouring prediction block covering the location ( xNbY, yNbY )
        internal bool CheckPredictionAvaliability(int xCb, int yCb, int nCbS, int xPb, int yPb, int nPbW, int nPbH, int partIdx, int xNbY, int yNbY)
        {
            bool sameCb =
                (xCb <= xNbY) && (yCb <= yNbY) &&
                (xCb + nCbS > xNbY) && (yCb + nCbS > yNbY);

            bool availableN;

            if (!sameCb)
                availableN = CheckZScanAvaliability(xPb, yPb, xNbY, yNbY);
            else if (
                nPbW << 1 == nCbS &&
                nPbH << 1 == nCbS &&
                partIdx == 1 &&
                yCb + nPbH <= yNbY &&
                xCb + nPbW > xNbY
                )
            {
                availableN = false;
            }
            else
                availableN = true;

            if (availableN && CuPredMode[xNbY, yNbY] == PredMode.MODE_INTRA)
                availableN = false;

            return availableN;
        }

        internal void SetCtDepth(int x0, int y0, int log2CbSize, int cqtDepth)
        {
            int nCbS = 1 << log2CbSize;
            CtDepth.Fill(x0, y0, nCbS, cqtDepth);
        }
        internal void SetCuPredMode(int x0, int y0, int nCbS, PredMode mode)
        {
            CuPredMode.Fill(x0, y0, nCbS, mode);
            if (mode == PredMode.MODE_SKIP)
            {
                for (int i = x0; i < x0 + nCbS; i++)
                {
                    for (int j = y0; j < y0 + nCbS; j++)
                    {
                        merge_flag[i, j] = true;
                    }
                }
            }
        }
        internal void SetPcmFlag(int x0, int y0, int nCbS, bool value)
        {
            for (int i = x0; i < x0 + nCbS; i++)
                for (int j = y0; j < y0 + nCbS; j++)
                    pcm_flag[i, j] = value;
        }
        internal void SetQpY(int x0, int y0, int log2CbSize, int qpY)
        {
            int nCbS = 1 << log2CbSize;
            QpY.Fill(x0, y0, nCbS, qpY);
        }
        internal void SetLog2CbSize(int x0, int y0, int log2CbSize)
        {
            int nCbS = 1 << log2CbSize;
            Log2CbSize.Fill(x0, y0, nCbS, log2CbSize);
        }

        // 8.4.2 Derivation process for luma intra prediction mode
        internal IntraPredMode[] GenerateIntraPredModeCandidates(int xPb, int yPb, int pbOffset, bool availableA, bool availableB)
        {
            IntraPredMode candIntraPredModeA, candIntraPredModeB;

            if (availableA == false)
            {
                candIntraPredModeA = IntraPredMode.INTRA_DC;
            }
            else if (CuPredMode[xPb - 1, yPb] != PredMode.MODE_INTRA || pcm_flag[xPb - 1, yPb])
            {
                candIntraPredModeA = IntraPredMode.INTRA_DC;
            }
            else
            {
                candIntraPredModeA = IntraPredModeY[xPb - 1, yPb];
            }

            if (availableB == false)
            {
                candIntraPredModeB = IntraPredMode.INTRA_DC;
            }
            else if (CuPredMode[xPb, yPb - 1] != PredMode.MODE_INTRA || pcm_flag[xPb, yPb - 1])
            {
                candIntraPredModeB = IntraPredMode.INTRA_DC;
            }
            else if (yPb - 1 < ((yPb >> sps.CtbLog2SizeY) << sps.CtbLog2SizeY))
            {
                candIntraPredModeB = IntraPredMode.INTRA_DC;
            }
            else
            {
                candIntraPredModeB = IntraPredModeY[xPb, yPb - 1];
            }

            IntraPredMode[] candModeList = Workspace.IntraPredModeCandidates;

            if (candIntraPredModeB == candIntraPredModeA)
            {
                if ((int)candIntraPredModeA < 2)
                {
                    candModeList[0] = IntraPredMode.INTRA_PLANAR;
                    candModeList[1] = IntraPredMode.INTRA_DC;
                    candModeList[2] = IntraPredMode.INTRA_ANGULAR26;
                }
                else
                {
                    candModeList[0] = candIntraPredModeA;
                    candModeList[1] = (IntraPredMode)(2 + (((int)candIntraPredModeA + 29) % 32));
                    candModeList[2] = (IntraPredMode)(2 + (((int)candIntraPredModeA - 2 + 1 ) % 32 ));
                }
            }
            else
            {
                candModeList[0] = candIntraPredModeA;
                candModeList[1] = candIntraPredModeB;

                if (candModeList[0] != IntraPredMode.INTRA_PLANAR && candModeList[1] != IntraPredMode.INTRA_PLANAR)
                {
                    candModeList[2] = IntraPredMode.INTRA_PLANAR;
                }
                else if (candModeList[0] != IntraPredMode.INTRA_DC && candModeList[1] != IntraPredMode.INTRA_DC)
                {
                    candModeList[2] = IntraPredMode.INTRA_DC;
                }
                else
                {
                    candModeList[2] = IntraPredMode.INTRA_ANGULAR26;
                }
            }

            return candModeList;
        }

        internal IntraPredMode SetIntraPredModeY(int xPb, int yPb, int pbSize, IntraPredMode[] candModeList)
        {
            IntraPredMode mode;

            if (prev_intra_luma_pred_flag[xPb, yPb])
            {
                mode = candModeList[mpm_idx[xPb, yPb]];
            }
            else
            {
                if (candModeList[0] > candModeList[1])
                    (candModeList[0], candModeList[1]) = (candModeList[1], candModeList[0]);

                if (candModeList[0] > candModeList[2])
                    (candModeList[0], candModeList[2]) = (candModeList[2], candModeList[0]);

                if (candModeList[1] > candModeList[2])
                    (candModeList[1], candModeList[2]) = (candModeList[2], candModeList[1]);

                mode = rem_intra_luma_pred_mode[xPb, yPb];

                for (int i = 0; i < 3; i++)
                {
                    if (mode >= candModeList[i])
                        mode++;
                }
            }

            IntraPredModeY.Fill(xPb, yPb, pbSize, mode);

            return mode;
        }
        internal void SetIntraPredModeC(int xPb, int yPb, int pbSize, IntraPredMode mode)
        {
            IntraPredModeC.Fill(xPb, yPb, pbSize, mode);
        }

        internal IntraPredMode DeriveIntraPredModeIndex(IntraPredMode intraLumaPredMode, byte intraChromaPredMode)
        {
            if (intraChromaPredMode == 4)
                return intraLumaPredMode;

            IntraPredMode candidate;

            switch (intraChromaPredMode)
            {
                case 0:
                    candidate = IntraPredMode.INTRA_PLANAR;
                    break;
                case 1:
                    candidate = IntraPredMode.INTRA_ANGULAR26;
                    break;
                case 2:
                    candidate = IntraPredMode.INTRA_ANGULAR10;
                    break;
                case 3:
                default:
                    candidate = IntraPredMode.INTRA_DC;
                    break;
            }

            if (intraLumaPredMode == candidate)
                return IntraPredMode.INTRA_ANGULAR34;

            return candidate;
        }
    }
}
