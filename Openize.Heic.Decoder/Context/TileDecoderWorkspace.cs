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
    internal class TileDecoderWorkspace
    {
        private PictureWorkspace picture;

        internal PictureWorkspace GetPictureWorkspace(seq_parameter_set_rbsp sps)
        {
            if (picture is null || !picture.Matches(sps))
            {
                picture = new PictureWorkspace(sps);
            }
            else
            {
                picture.Clear();
            }
            return picture;
        }
    }
}
