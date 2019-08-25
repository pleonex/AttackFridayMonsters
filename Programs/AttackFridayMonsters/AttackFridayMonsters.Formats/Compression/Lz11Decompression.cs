//  Lz11Decompression.cs
//
//  Copyright (c) 2011 CUE (author of the C library)
//  Copyright (c) 2019 SceneGate Team
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
namespace AttackFridayMonsters.Formats.Compression
{
    using System;
    using Yarhl.FileFormat;
    using Yarhl.IO;

    /// <summary>
    /// Decompress a stream with the LZ11 algorithm.
    /// </summary>
    /// <remarks>
    /// Based on the Nintendo DS / GBA Compressors utilities by CUE
    /// * https://romxhack.esforos.com/viewtopic.php?f=2&t=117
    /// * https://www.romhacking.net/utilities/826/
    /// </remarks>
    public class Lz11Decompression : IConverter<BinaryFormat, BinaryFormat>
    {
        const byte Id = 0x11; // Also known as LZX big endian
        const int MinimumSize = 4;
        const int MaximumSize = 0x01400000; // 20 MB

        const int Threshold = 2;      // Max number of bytes to not encode
        const int Threshold1 = 0x10;  // Max coded (1 << 4)
        const int Threshold2 = 0x110; // Max coded (1 << 4) + (1 << 8)

        byte[] buffer;
        int idx;

        byte[] output;
        int outIdx;

        int decompressedLength;
        byte mask;
        int flags;

        public BinaryFormat Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source.Stream.Length < MinimumSize)
                throw new FormatException("Required more data");
            if (source.Stream.Length > MaximumSize)
                throw new FormatException("Too much data");

            buffer = new byte[source.Stream.Length];
            source.Stream.Position = 0;
            source.Stream.Read(buffer, 0, buffer.Length);
            idx = 0;
            ReadHeader();

            output = new byte[decompressedLength];
            outIdx = 0;

            while (outIdx < decompressedLength) {
                if (GetNextFlag()) {
                    CopyByte();
                } else {
                    CopyDecompressedSequence();
                }
            }

            var stream = DataStreamFactory.FromStream(new System.IO.MemoryStream(output));
            return new BinaryFormat(stream);
        }

        void ReadHeader()
        {
            if (buffer[idx++] != Id)
                throw new FormatException("Stream is not LZ11 encoded");

            decompressedLength = buffer[idx++] | (buffer[idx++] << 8) | (buffer[idx++] << 16);
            mask = 0;
        }

        bool GetNextFlag()
        {
            // Advance the mask to check next bit
            mask >>= 1;

            // Get a new set of flags and restore mask
            if (mask == 0) {
                mask = 0x80;  // Mask for each of the 8 bits
                flags = buffer[idx++];
            }

            return (flags & mask) == 0;
        }

        void CopyByte()
        {
            output[outIdx++] = buffer[idx++];
        }

        void CopyDecompressedSequence()
        {
            // First 4 bits specify the size of the length parameter.
            // The position parameter is always 12 bits.
            int flag = buffer[idx] >> 4;

            uint info;
            int threshold;
            if (flag >= Threshold) {
                info = (uint)(buffer[idx++] << 8) | buffer[idx++];
                threshold = 0;
            } else if (flag == 0) {
                info = (uint)(((buffer[idx++] & 0x0F) << 16) | (buffer[idx++] << 8) | buffer[idx++]);
                threshold = Threshold1;
            } else {
                info = (uint)(((buffer[idx++] & 0x0F) << 24) | (buffer[idx++] << 16) | (buffer[idx++] << 8) | buffer[idx++]);
                threshold = Threshold2;
            }

            uint position = (info & 0xFFF) + 1;
            uint length = (uint)((info >> 12) + threshold + 1);

            // Read and write byte by byte because the byte to read may be
            // written in just the previous iteration.
            for (int i = 0; i < length; i++) {
                output[outIdx] = output[outIdx - position];
                outIdx++;
            }
        }
    }
}
