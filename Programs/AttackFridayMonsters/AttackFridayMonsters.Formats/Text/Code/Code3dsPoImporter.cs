//  Copyright (c) 2020 Benito Palacios Sánchez
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
namespace AttackFridayMonsters.Formats.Text.Code
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Yarhl.FileFormat;
    using Yarhl.IO;
    using Yarhl.Media.Text;

    public class Code3dsPoImporter :
        IInitializer<(Po, DataStream)>, IConverter<BinaryFormat, BinaryFormat>
    {
        Po texts;
        DataStream exHeader;
        ExtendedHeaderObjInfo codeInfo;

        long ramOffset;
        DataWriter writer;

        public void Initialize((Po, DataStream) parameters)
        {
            texts = parameters.Item1;
            exHeader = parameters.Item2;
            codeInfo = ReadExtendedHeader(exHeader);
            ramOffset = codeInfo.TextSection.RamAddress;
        }

        public BinaryFormat Convert(BinaryFormat source)
        {
            writer = new DataWriter(source.Stream);

            bool updateExHeader = false;
            foreach (var entry in texts.Entries) {
                StringDefinition definition = GetDefinition(entry.Reference);
                byte[] text = EncodeText(entry.Text, definition.Encoding);

                if (TryImportInPlace(text, definition)) {
                    continue;
                }

                updateExHeader = true;
                if (!TryImportInPadding(text, definition)) {
                    throw new FormatException($"The text doesn't fit in file {entry.Text}");
                }
            }

            if (updateExHeader) {
                WriteExtendedHeader(exHeader, codeInfo);
            }

            return source;
        }

        static ExtendedHeaderObjInfo ReadExtendedHeader(DataStream stream)
        {
            var reader = new DataReader(stream);
            var info = new ExtendedHeaderObjInfo();

            stream.Position = 0x10;
            info.TextSection = new SectionInfo {
                RamAddress = reader.ReadUInt32(),
                PhysicalSize = reader.ReadUInt32() << 12,
                Size = reader.ReadUInt32(),
            };

            stream.Position = 0x20;
            info.ReadOnlySection = new SectionInfo {
                RamAddress = reader.ReadUInt32(),
                PhysicalSize = reader.ReadUInt32() << 12,
                Size = reader.ReadUInt32(),
            };

            stream.Position = 0x30;
            info.DataSection = new SectionInfo {
                RamAddress = reader.ReadUInt32(),
                PhysicalSize = reader.ReadUInt32() << 12,
                Size = reader.ReadUInt32(),
            };


            return info;
        }

        static void WriteExtendedHeader(DataStream stream, ExtendedHeaderObjInfo info)
        {
            var writer = new DataWriter(stream);

            stream.Position = 0x10;
            writer.Write((uint)info.TextSection.RamAddress);
            writer.Write((uint)(info.TextSection.PhysicalSize >> 12));
            writer.Write((uint)info.TextSection.Size);

            stream.Position = 0x20;
            writer.Write((uint)info.ReadOnlySection.RamAddress);
            writer.Write((uint)(info.ReadOnlySection.PhysicalSize >> 12));
            writer.Write((uint)info.ReadOnlySection.Size);

            stream.Position = 0x30;
            writer.Write((uint)info.DataSection.RamAddress);
            writer.Write((uint)(info.DataSection.PhysicalSize >> 12));
            writer.Write((uint)info.DataSection.Size);
        }

        static StringDefinition GetDefinition(string reference)
        {
            string[] segments = reference.Split(':');
            if (segments.Length != 4) {
                throw new FormatException($"Invalid number of segments: {reference}");
            }

            var pointers = segments[3].Split(',')
                .Select(x => long.Parse(x.Substring(2), NumberStyles.HexNumber));

            return new StringDefinition {
                Address = int.Parse(segments[0].Substring(2), NumberStyles.HexNumber),
                Size = int.Parse(segments[1]),
                Encoding = segments[2],
                Pointers = new Collection<long>(pointers.ToList())
            };
        }

        static byte[] EncodeText(string text, string encodingName)
        {
            var encoding = Encoding.GetEncoding(encodingName);
            return encoding.GetBytes(text + '\0');
        }

        static SectionInfo FindSection(long address, params SectionInfo[] sections)
        {
            foreach (var section in sections) {
                if (address >= section.RamAddress && address < section.RamAddress + section.Size) {
                    return section;
                }
            }

            throw new FormatException($"Invalid address: 0x{address:X8}");
        }

        bool TryImportInPlace(byte[] text, StringDefinition definition)
        {
            if (text.Length > definition.Size) {
                return false;
            }

            writer.Stream.Position = definition.Address - ramOffset;
            writer.Write(text);
            return true;
        }

        bool TryImportInPadding(byte[] text, StringDefinition definition)
        {
            SectionInfo section = FindSection(
                definition.Address,
                codeInfo.TextSection,
                codeInfo.ReadOnlySection,
                codeInfo.DataSection);

            if (section.PaddingSize < text.Length) {
                return false;
            }

            // Put at the end
            long oldAddress = definition.Address;
            definition.Address = section.RamAddress + section.Size;
            writer.Stream.Position = definition.Address - ramOffset;
            writer.Write(text);
            section.Size += text.Length;

            // Update pointers
            var reader = new DataReader(writer.Stream);
            foreach (var pointerAddr in definition.Pointers) {
                // Skip indirect pointers (like accessing from code sums).
                // It may be because it's a reference to another pointer
                // or because it was the result of code (e.g. additions).
                // It may require manual code changes.
                reader.Stream.Position = pointerAddr - ramOffset;
                uint currentPtr = reader.ReadUInt32();
                if (currentPtr != oldAddress) {
                    Console.WriteLine($"WARNING: Indirect pointer for: 0x{oldAddress:X8}");
                    continue;
                }

                writer.Stream.Position = pointerAddr - ramOffset;
                writer.Write((uint)definition.Address);
            }

            return true;
        }

        private sealed class ExtendedHeaderObjInfo
        {
            public SectionInfo TextSection { get; set; }

            public SectionInfo ReadOnlySection { get; set; }

            public SectionInfo DataSection { get; set; }
        }

        private sealed class SectionInfo
        {
            public long RamAddress { get; set; }

            public long PhysicalSize { get; set; }

            public long Size { get; set; }

            public long PaddingSize => PhysicalSize - Size;
        }
    }
}
