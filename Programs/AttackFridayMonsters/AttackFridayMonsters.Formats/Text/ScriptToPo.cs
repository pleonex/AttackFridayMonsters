//
//  ScriptToPo.cs
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
    using System.Text;
    using Yarhl.FileFormat;
    using Yarhl.IO;
    using Yarhl.Media.Text;

    public class ScriptToPo : IConverter<BinaryFormat, Po>
    {
        public Po Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            DataReader reader = new DataReader(source.Stream);
            Po po = new Po {
                Header = new PoHeader(
                    "Attack of the Friday Monsters Translatation",
                    "benito356@gmail.com")
            };

            uint numBlocks = reader.ReadUInt32();
            for (int b = 0; b < numBlocks; b++) {
                // Block info
                source.Stream.Seek(4 + (b * 0x0C), SeekMode.Start);
                reader.ReadUInt32(); // unknown
                reader.ReadUInt32(); // block size
                uint blockOffset = reader.ReadUInt32();

                // Sections
                source.Stream.Seek(blockOffset, SeekMode.Start);
                uint numSections = reader.ReadUInt32();
                if (numSections < 3)
                    throw new FormatException($"Missing sections in block {b}");

                // We skip the first 3 sections with unknown content
                bool firstSection = true;
                for (int s = 3; s < numSections; s++) {
                    source.Stream.Seek(blockOffset + 4 + (s * 4), SeekMode.Start);
                    uint sectionOffset = reader.ReadUInt32();
                    if (sectionOffset == 0)
                        continue;

                    source.Stream.Seek(blockOffset + sectionOffset, SeekMode.Start);
                    short unk = reader.ReadInt16();
                    if (unk == 0x4D30)
                        continue;

                    PoEntry entry = new PoEntry() {
                        Original = ReadTokenizedString(reader),
                        Context = $"Block:{b},Section:{s},Unk:{unk}",
                        ExtractedComments = firstSection ? "Dialog start" : null
                    };

                    po.Add(entry);
                    firstSection = false;
                }
            }

            return po;
        }

        string ReadTokenizedString(DataReader reader)
        {
            StringBuilder text = new StringBuilder();
            Encoding encoding = Encoding.GetEncoding("utf-16");

            ushort data = reader.ReadUInt16();
            while (data != 0x1F) {
                if (data == 0x2001) {
                    text.Append("<UNK2>");
                } else if (data == 0x1E) {
                    ushort unk1 = reader.ReadUInt16();
                    text.Append($"<UNK1:{unk1}>");
                } else if (data == 0x0D) {
                    text.AppendLine();
                } else {
                    text.Append(encoding.GetString(BitConverter.GetBytes(data)));
                }

                data = reader.ReadUInt16();
            }

            return text.ToString();
        }
    }
}
