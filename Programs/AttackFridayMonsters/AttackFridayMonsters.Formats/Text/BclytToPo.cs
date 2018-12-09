//  BclytToPo.cs
//
//  Author:
//       Benito Palacios Sanchez <benito356@gmail.com>
//
//  Copyright (c) 2017 Benito Palacios Sanchez
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
namespace AttackFridayMonsters.Formats.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Yarhl.FileFormat;
    using Yarhl.IO;
    using Yarhl.Media.Text;

    public class BclytToPo :
        IConverter<BinaryFormat, Po>,
        IConverter<Tuple<BinaryFormat, Po>, BinaryFormat>
    {
        public BinaryFormat Convert(Tuple<BinaryFormat, Po> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source.Item1 == null)
                throw new ArgumentNullException(nameof(BinaryFormat));
            if (source.Item2 == null)
                throw new ArgumentNullException(nameof(Po));

            BinaryFormat binary = new BinaryFormat();
            DataWriter writer = new DataWriter(binary.Stream);

            DataReader reader = new DataReader(source.Item1.Stream);
            reader.Stream.Position = 0;

            // Header
            writer.Write(reader.ReadString(4), false); // magic stamp
            writer.Write(reader.ReadUInt16());  // endianness
            writer.Write(reader.ReadUInt32());  // header size
            writer.Write(reader.ReadUInt16());  // version

            // placeholder for size
            binary.Stream.PushCurrentPosition();
            writer.Write(0x00);
            reader.ReadUInt32();

            uint numSections = reader.ReadUInt32();
            writer.Write(numSections);

            Encoding encoding = Encoding.GetEncoding("utf-16");
            Queue<PoEntry> entries = new Queue<PoEntry>(source.Item2.Entries);
            for (int i = 0; i < numSections; i++) {
                string section = reader.ReadString(4);
                int size = reader.ReadInt32();

                // Skip other sections
                if (section != "txt1" || size == 0x74) {
                    writer.Write(section, false);
                    writer.Write(size);
                    writer.Write(reader.ReadBytes(size - 0x08));
                    continue;
                }

                // Get encoded text
                PoEntry entry = entries.Dequeue();
                string text = string.IsNullOrEmpty(entry.Translated) ?
                                    entry.Original : entry.Translated;

                // Write new section
                writer.Write("txt1", false);
                long sizePosition = binary.Stream.Position;
                writer.Write(0x00);
                writer.Write(reader.ReadBytes(0x6C));

                reader.ReadString(encoding);
                writer.Write(text, true, encoding);

                writer.WritePadding(0x00, 4);
                while (reader.Stream.Position % 4 != 0)
                    reader.ReadByte();

                long endPosition = binary.Stream.Position;
                binary.Stream.Position = sizePosition;
                writer.Write((uint)(endPosition - binary.Stream.Position + 4));
                binary.Stream.Position = endPosition;
            }

            binary.Stream.PopPosition();
            writer.Write((uint)binary.Stream.Length);

            return binary;
        }

        public Po Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            DataReader reader = new DataReader(source.Stream);
            Po po = new Po {
                Header = new PoHeader(
                    "Attack of the Friday Monsters Translation",
                    "benito356@gmail.com",
                    "es-ES"),
            };

            if (reader.ReadString(4) != "CLYT")
                throw new FormatException("Invalid magic stamp");
            if (reader.ReadUInt16() != 0xFEFF)
                throw new FormatException("Invalid endianess");
            uint headerSize = reader.ReadUInt32();
            reader.ReadUInt16(); // version
            reader.ReadUInt32(); // file size
            uint numSections = reader.ReadUInt32();

            for (int i = 0; i < numSections; i++) {
                string section = reader.ReadString(4);
                uint size = reader.ReadUInt32();
                if (section != "txt1") {
                    reader.Stream.Position += size - 0x08;
                    continue;
                }

                uint unk1 = reader.ReadUInt32();
                string font = reader.ReadString(0x18).Replace("\0", string.Empty);
                float[] unk2 = new float[10];
                for (int j = 0; j < 10; j++)
                    unk2[j] = BitConverter.ToSingle(reader.ReadBytes(4), 0);

                byte[] data = reader.ReadBytes(0x6C - 0x18 - 4 - (10 * 4));

                Encoding encoding = Encoding.GetEncoding("utf-16");
                string text = string.Empty;
                if (size > 0x74)
                    text = reader.ReadString(encoding);
                var entry = new PoEntry(text);
                entry.Context = $"s:{i}";
                entry.ExtractedComments = $"1:{unk1:X8},font:{font},points:{unk2[0]},{unk2[1]},{unk2[2]},{unk2[3]},{unk2[4]},{unk2[5]},{unk2[6]},{unk2[7]},{unk2[8]},{unk2[9]},data:{BitConverter.ToString(data)}";
                po.Add(entry);

                while (reader.Stream.Position % 4 != 0)
                    reader.ReadByte();
            }

            return po;
        }
    }
}
