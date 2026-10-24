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
    /// <summary>
    /// Class used to convert colors from YUV colorspace to RGB.
    /// </summary>
    internal class YuvConverter
    {
        /// <summary>
        /// Full or TV range flag.
        /// </summary>
        bool fullRangeFlag;

        /// <summary>
        /// Coded image.
        /// </summary>
        HeicImageFrame picture;

        double Kr;
        double Kb;
        double Kg;

        double cooffCrToR;
        double cooffCrToG;
        double cooffCbToG;
        double cooffCbToB;

        public YuvConverter(HeicImageFrame picture)
        {
            fullRangeFlag = picture.hvcConfig.SPS.vui_parameters.video_full_range_flag;

            this.picture = picture;

            DefineCoefficients(picture.hvcConfig.SPS.vui_parameters);
        }


        /// <summary>
        /// Convert YUV samples to RGBA pixels.
        /// </summary>
        /// <returns>RGBA pixels.</returns>
        public RgbaImage GetRgbaImage()
        {
            seq_parameter_set_rbsp sps = picture.hvcConfig.SPS;
            int width = (int)sps.pic_width_in_luma_samples;
            int height = (int)sps.pic_height_in_luma_samples;
            RgbaImage image = new RgbaImage(width, height);
            byte[] pixels = image.Pixels;

            if (picture.rawPixels is null && picture.rawPixelsHighColorRange is null)
            {
                return image;
            }

            bool highColorRange = sps.BitDepthY > 8 || sps.BitDepthC > 8;
            bool hasChroma = sps.ChromaArrayType != 0;
            int subWidth = sps.SubWidthC;
            int subHeight = sps.SubHeightC;
            int chromaHalfRange = 1 << (sps.BitDepthC - 1);
            int lumaOffset = 16 << (sps.BitDepthY - 8);
            double lumaScale = hasChroma ? 256.0 / (1 << sps.BitDepthY) : 1.0;
            double chromaScale = 256.0 / (1 << sps.BitDepthC);
            const double TvRangeLuma = 255.0 / 219;
            const double TvRangeChroma = 255.0 / 224;
            byte[,] luma8 = highColorRange ? null : picture.rawPixels[0];
            ushort[,] lumaHigh = highColorRange ? picture.rawPixelsHighColorRange[0] : null;
            byte[,] cb8 = !highColorRange && hasChroma ? picture.rawPixels[1] : null;
            byte[,] cr8 = !highColorRange && hasChroma ? picture.rawPixels[2] : null;
            ushort[,] cbHigh = highColorRange && hasChroma ? picture.rawPixelsHighColorRange[1] : null;
            ushort[,] crHigh = highColorRange && hasChroma ? picture.rawPixelsHighColorRange[2] : null;

            for (int row = 0; row < height; row++)
            {
                int index = row * image.Stride;
                int chromaRow = row / subHeight;
                for (int column = 0; column < width; column++)
                {
                    double luma = highColorRange ? lumaHigh[column, row] : luma8[column, row];
                    if (!fullRangeFlag)
                    {
                        luma = TvRangeLuma * (luma - lumaOffset);
                    }
                    double red = luma;
                    double green = luma;
                    double blue = luma;
                    if (hasChroma)
                    {
                        int chromaColumn = column / subWidth;
                        double cb = (highColorRange ? cbHigh[chromaColumn, chromaRow] : cb8[chromaColumn, chromaRow]) - chromaHalfRange;
                        double cr = (highColorRange ? crHigh[chromaColumn, chromaRow] : cr8[chromaColumn, chromaRow]) - chromaHalfRange;
                        if (!fullRangeFlag)
                        {
                            cb = TvRangeChroma * cb;
                            cr = TvRangeChroma * cr;
                        }
                        luma *= lumaScale;
                        cb *= chromaScale;
                        cr *= chromaScale;
                        red = luma + cooffCrToR * cr;
                        green = luma - cooffCrToG * cr - cooffCbToG * cb;
                        blue = luma + cooffCbToB * cb;
                    }
                    pixels[index] = (byte)MathExtra.Clip3(0, 255, (int)red);
                    pixels[index + 1] = (byte)MathExtra.Clip3(0, 255, (int)green);
                    pixels[index + 2] = (byte)MathExtra.Clip3(0, 255, (int)blue);
                    pixels[index + 3] = 255;
                    index += RgbaImage.BytesPerPixel;
                }
            }
            return image;
        }

        /// <summary>
        /// Define converter coefficients based on parameters of image meta data.
        /// </summary>
        /// <param name="vui_parameters">Image usability information data.</param>
        private void DefineCoefficients(vui_parameters vui_parameters)
        {
            //vui_parameters.video_full_range_flag;
            //vui_parameters.colour_primaries;
            //vui_parameters.transfer_characteristics;
            //vui_parameters.matrix_coeffs;

            switch (vui_parameters.matrix_coeffs)
            {
                case 1:
                    Kr = 0.2126;
                    Kb = 0.0722;
                    break;
                case 4:
                    Kr = 0.30;
                    Kb = 0.11;
                    break;
                case 5:
                case 6:
                    Kr = 0.299;
                    Kb = 0.114;
                    break;
                case 7:
                    Kr = 0.212;
                    Kb = 0.087;
                    break;
                case 9:
                case 10:
                    Kr = 0.2627;
                    Kb = 0.0593;
                    break;

                case 12:
                case 13:
                    var p = ColorPrimaries.GetSpecified(vui_parameters.colour_primaries);

                    double zR = 1 - (p.xR + p.yR);
                    double zG = 1 - (p.xG + p.yG);
                    double zB = 1 - (p.xB + p.yB);
                    double zW = 1 - (p.xW + p.yW);

                    double denom = p.yW * (p.xR * (p.yG * zB - p.yB * zG) + p.xG * (p.yB * zR - p.yR * zB) + p.xB * (p.yR * zG - p.yG * zR));

                    try
                    {
                        Kr = (p.yR * (p.xW * (p.yG * zB - p.yB * zG) + p.yW * (p.xB * zG - p.xG * zB) + zW * (p.xG * p.yB - p.xB * p.yG))) / denom;
                        Kb = (p.yB * (p.xW * (p.yR * zG - p.yG * zR) + p.yW * (p.xG * zR - p.xR * zG) + zW * (p.xR * p.yG - p.xG * p.yR))) / denom;
                    }
                    catch
                    {
                        Kr = 0.299;
                        Kb = 0.114;
                    }
                    break;

                case 0:  // Identity; See Equations E-31 to E-33
                case 2:  // Unspecified
                case 3:  // Reserved
                case 8:  // YCgCo;    See Equations E-28 to E-30
                case 11: // Y′D′ZD′X; See Equations E-59 to E-61
                case 14: // ICTCP
                default:
                    Kr = 0.299;
                    Kb = 0.114;
                    break;

            }

            Kg = 1 - Kr - Kb;

            cooffCrToR = 2 * (1 - Kr);
            cooffCrToG = 2 * Kr * (1 - Kr) / Kg;
            cooffCbToG = 2 * Kb * (1 - Kb) / Kg;
            cooffCbToB = 2 * (1 - Kb);
        }


        internal class ColorPrimaries
        {
            public double xG;
            public double yG;
            public double xB;
            public double yB;
            public double xR;
            public double yR;
            public double xW;
            public double yW;

            public ColorPrimaries(double xG, double yG, double xB, double yB, double xR, double yR, double xW, double yW)
            {
                this.xG = xG;
                this.yG = yG;
                this.xB = xB;
                this.yB = yB;
                this.xR = xR;
                this.yR = yR;
                this.xW = xW;
                this.yW = yW;
            }

            public static ColorPrimaries GetSpecified(int colourPrimaries)
            {
                switch (colourPrimaries)
                {
                    case 1:
                        return new ColorPrimaries(0.300, 0.600, 0.150, 0.060, 0.640, 0.330, 0.3127, 0.3290);
                    case 4:
                        return new ColorPrimaries(0.210, 0.710, 0.140, 0.080, 0.670, 0.330, 0.3100, 0.3160);
                    case 5:
                        return new ColorPrimaries(0.290, 0.600, 0.150, 0.060, 0.640, 0.330, 0.3127, 0.3290);
                    case 6:
                    case 7:
                        return new ColorPrimaries(0.310, 0.595, 0.155, 0.070, 0.630, 0.340, 0.3127, 0.3290);
                    case 8:
                        return new ColorPrimaries(0.243, 0.692, 0.145, 0.049, 0.681, 0.319, 0.3100, 0.3160);
                    case 9:
                        return new ColorPrimaries(0.170, 0.797, 0.131, 0.046, 0.708, 0.292, 0.3127, 0.3290);
                    case 10:
                        return new ColorPrimaries(0.000, 1.000, 0.000, 0.000, 1.000, 0.000, 0.333333, 0.33333);
                    case 11:
                        return new ColorPrimaries(0.265, 0.690, 0.150, 0.060, 0.680, 0.320, 0.3140, 0.3510);
                    case 12:
                        return new ColorPrimaries(0.265, 0.690, 0.150, 0.060, 0.680, 0.320, 0.3127, 0.3290);
                    case 22:
                        return new ColorPrimaries(0.295, 0.605, 0.155, 0.077, 0.630, 0.340, 0.3127, 0.3290);
                    case 2:  // Unspecified
                    default: // Reserved
                        return null;
                }
            }
        }
    }
}
