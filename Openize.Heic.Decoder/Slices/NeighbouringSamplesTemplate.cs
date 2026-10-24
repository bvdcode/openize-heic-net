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
    internal class NeighbouringSamplesTemplate<T>
    {
        protected readonly T[] data;
        protected int size;

        public NeighbouringSamplesTemplate(int maximumSize)
        {
            data = new T[4 * maximumSize + 1];
            size = maximumSize;
        }

        public void SetSize(int transformSize)
        {
            size = transformSize;
        }

        public T this[int x, int y]
        {
            get => data[GetIndex(x, y)];
            set => data[GetIndex(x, y)] = value;
        }

        private int GetIndex(int x, int y)
        {
            if (y == -1)
            {
                return x + 1;
            }
            return 2 * size + y + 1;
        }
    }
}
