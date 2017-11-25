//
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
    using Yarhl.IO;
    using Yarhl.FileFormat;
    using Yarhl.Media.Text;

    public class BclytToPo :
        IConverter<BinaryFormat, Po>,
        IConverter<Po, BinaryFormat>
    {

        public DataStream Original { get; set; }

        public BinaryFormat Convert(Po source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (Original == null)
                throw new ArgumentNullException(nameof(Original));

            BinaryFormat binary = new BinaryFormat();
            DataWriter writer = new DataWriter(binary.Stream);

            Original.Position = 0;
            DataReader reader = new DataReader(Original);

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
            Queue<PoEntry> entries = new Queue<PoEntry>(source.Entries);
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
                    "benito356@gmail.com")
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

                reader.Stream.Position += 0x6C;
                if (size > 0x74) {
                    Encoding encoding = Encoding.GetEncoding("utf-16");
                    string text = reader.ReadString(encoding);
                    po.Add(new PoEntry(text) { Context = $"s:{i}" });

                    while (reader.Stream.Position % 4 != 0)
                        reader.ReadByte();
                }
            }

            return po;
        }
    }
}
