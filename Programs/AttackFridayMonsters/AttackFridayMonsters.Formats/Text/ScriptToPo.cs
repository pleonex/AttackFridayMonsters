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
    using System.Collections.Generic;
    using System.Text;
    using Yarhl.FileFormat;
    using Yarhl.IO;
    using Yarhl.Media.Text;

    public class ScriptToPo :
        IConverter<BinaryFormat, Po>,
        IConverter<Po, BinaryFormat>
    {
        static readonly Dictionary<int, string> Characters = new Dictionary<int, string> {
            { 0xFFFF, "Narrator" },
            { 0x000, "Sohta" }, { 0x100, "Cleaner Man Junior" },
            { 0x001, "Dad" }, { 0x101, "Cleaner Man" },
            { 0x002, "Mom" },
            { 0x003, "S-chan" }, { 0x103, "Lady Silvia" },
            { 0x004, "Odd Man" }, { 0x104, "Frank" },
            { 0x005, "Strange Lady" }, { 0x105, "Megami-chan" },
            { 0x006, "Police Officer" }, { 0x106, "Officer Kobayashi" },
            { 0x007, "Bakery Lady" }, { 0x107, "Emily" },
            { 0x008, "Owner" }, { 0x108, "Ramen's Dad" },
            { 0x009, "Energetic Boy" }, { 0x109, "Ramen" },
            { 0x00A, "Black Shirt Man" },
            { 0x00B, "Cute Girl" }, { 0x10B, "Akebi" },
            { 0x00C, "Bad Kid" }, { 0x10C, "Nanafushi" },
            { 0x00D, "Bad Kid's Servant" }, { 0x10D, "Billboard" },
            { 0x00E, "Boy with Glasses" }, { 0x10E, "A Plus" },
            { 0x014, "Probably an Alien" },
        };

        public DataStream OriginalScript { get; set; }

        public BinaryFormat Convert(Po source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (OriginalScript == null)
                throw new FormatException("Missing original script");

            Queue<PoEntry> entries = new Queue<PoEntry>(source.Entries);

            BinaryFormat binary = new BinaryFormat();
            DataWriter writer = new DataWriter(binary.Stream);

            OriginalScript.Position = 0;
            DataReader reader = new DataReader(OriginalScript);

            // First thing is number of blocks / subscripts
            uint numBlocks = reader.ReadUInt32();
            writer.Write(numBlocks);

            // write empty fat to be able to write blocks at the same time
            writer.WriteTimes(0x00, 0x0C * numBlocks);
            binary.Stream.Position = 4;

            for (int b = 0; b < numBlocks; b++) {
                // Write current block FAT entry
                writer.Write(reader.ReadUInt32()); // ... event ID
                int origBlockSize = reader.ReadInt32(); // ... size
                writer.Write(0x00);  // placeholder
                uint origBlockOffset = reader.ReadUInt32(); // ... offset
                int blockOffset = (int)writer.Stream.Length;
                writer.Write(blockOffset);

                // Write it
                reader.Stream.PushToPosition(origBlockOffset, SeekMode.Start);
                writer.Stream.PushToPosition(0, SeekMode.End);
                WriteBlock(writer, reader, origBlockSize, entries);

                // Return to block FAT and update size
                reader.Stream.PopPosition();
                writer.Stream.PopPosition();

                writer.Stream.PushToPosition(-0x08, SeekMode.Current);
                writer.Write((uint)(writer.Stream.Length - blockOffset));
                writer.Stream.PopPosition();

                // Pad block (it doesn't count in block size)
                writer.Stream.PushToPosition(0, SeekMode.End);
                while ((writer.Stream.Position - blockOffset) % 0x10 != 0)
                    writer.Write((byte)0x00);
                writer.Stream.PopPosition();
            }

            return binary;
        }

        public Po Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            source.Stream.Seek(0, SeekMode.Start);
            DataReader reader = new DataReader(source.Stream);
            Po po = new Po {
                Header = new PoHeader(
                    "Attack of the Friday Monsters Translatation",
                    "benito356@gmail.com",
                    "es-ES"),
            };

            uint numBlocks = reader.ReadUInt32();
            for (int b = 0; b < numBlocks; b++) {
                // Block info
                reader.ReadUInt32(); // unknown
                reader.ReadUInt32(); // block size
                uint blockOffset = reader.ReadUInt32();

                // Sections
                source.Stream.PushToPosition(blockOffset, SeekMode.Start);
                uint numSections = reader.ReadUInt32();
                if (numSections < 3)
                    throw new FormatException($"Missing sections in block {b}");

                // We skip the first 3 sections with unknown content
                reader.ReadBytes(3 * 4);
                bool firstSection = true;
                for (int s = 3; s < numSections; s++) {
                    uint sectionOffset = reader.ReadUInt32();
                    if (sectionOffset == 0)
                        continue;

                    source.Stream.PushToPosition(blockOffset + sectionOffset, SeekMode.Start);
                    ushort charId = reader.ReadUInt16();
                    if (charId == 0x4D30) {
                        source.Stream.PopPosition();
                        continue;
                    }

                    if (!Characters.ContainsKey(charId))
                        throw new FormatException("Unknown char: " + charId);

                    PoEntry entry = new PoEntry {
                        Original = ReadTokenizedString(reader),
                        Context = $"b:{b}|s:{s}",
                        ExtractedComments = Characters[charId],
                    };

                    if (firstSection)
                        entry.ExtractedComments = "[Start] " + entry.ExtractedComments;

                    po.Add(entry);
                    firstSection = false;
                    source.Stream.PopPosition();
                }

                source.Stream.PopPosition();
            }

            return po;
        }

        static void WriteBlock(DataWriter writer, DataReader reader, int blockSize, Queue<PoEntry> entries)
        {
            long blockOffset = writer.Stream.Position;
            long origBlockOffset = reader.Stream.Position;

            // Process each section
            int numSections = reader.ReadInt32();
            writer.Write(numSections);
            int endFatPos = 4 + (4 * numSections);

            // Write empty offset list
            writer.Stream.PushCurrentPosition();
            writer.WriteTimes(0x00, 4 * numSections);
            writer.Stream.PopPosition();

            // Write the first three data unknown blocks
            // ... FAT
            writer.Write(reader.ReadBytes(3 * 4));
            writer.Stream.PushCurrentPosition();
            reader.Stream.PushCurrentPosition();

            // ... Data -- get the next offset
            int nextOffset = -1;
            for (int i = 3; i < numSections && nextOffset <= 0; i++)
                nextOffset = reader.ReadInt32();

            // ... if there isn't more offsets, then copy block size and that is
            if (nextOffset == -1)
                nextOffset = blockSize;

            // ... copy!
            writer.Stream.PushToPosition(0, SeekMode.End);
            reader.Stream.PushToPosition(origBlockOffset + endFatPos, SeekMode.Start);
            writer.Write(reader.ReadBytes(nextOffset - endFatPos));
            reader.Stream.PopPosition();
            writer.Stream.PopPosition();

            // Write the rest of sections
            writer.Stream.PopPosition();
            reader.Stream.PopPosition();
            for (int s = 3; s < numSections; s++) {
                int origSecOffset = reader.ReadInt32();
                if (origSecOffset == 0x00) {
                    writer.Write(0x00);
                    continue;
                }

                // Write the new offset and start writing section
                writer.Write((uint)(writer.Stream.Length - blockOffset));
                writer.Stream.PushToPosition(0, SeekMode.End);
                reader.Stream.PushToPosition(origBlockOffset + origSecOffset, SeekMode.Start);

                // Char ID
                ushort charId = reader.ReadUInt16();
                writer.Write(charId);

                // Text
                if (charId == 0x4D30) {
                    writer.Write(reader.ReadString(Encoding.ASCII));
                } else {
                    var entry = entries.Dequeue();
                    var text = string.IsNullOrEmpty(entry.Translated) ?
                                     entry.Original : entry.Translated;
                    WriteTokenizedString(text, writer);
                }

                // Let's add some padding
                writer.WritePadding(0x00, 4);

                reader.Stream.PopPosition();
                writer.Stream.PopPosition();
            }
        }

        static string ReadTokenizedString(DataReader reader)
        {
            StringBuilder text = new StringBuilder();
            Encoding encoding = Encoding.GetEncoding("utf-16");

            ushort data;
            while ((data = reader.ReadUInt16()) != 0x00) {
                if (data == 0x2001) {
                    text.Append("<emphasis>");
                } else if (data == 0x1E) {
                    ushort waitTime = reader.ReadUInt16();
                    text.AppendLine($"<pause:{waitTime}>");
                } else if (data == 0x1F) {
                    text.Append("<end>");

                    // At the end of a dialog there is a double end
                    // let's try to check that
                    if (reader.Stream.EndOfStream)
                        break;

                    ushort doubleEnd = reader.ReadUInt16();
                    reader.Stream.Position -= 2;
                    if (doubleEnd != 0x1F)
                        break;
                } else if (data == 0x0D) {
                    text.AppendLine();
                } else {
                    text.Append(encoding.GetString(BitConverter.GetBytes(data)));
                }
            }

            return text.ToString();
        }

        static void WriteTokenizedString(string text, DataWriter writer)
        {
            for (int i = 0; i < text.Length; i++) {
                if (text[i] == '<') {
                    if (text.Substring(i).StartsWith("<emphasis>")) {
                        writer.Write((ushort)0x2001);
                        i += "emphasis>".Length;
                    } else if (text.Substring(i).StartsWith("<end>")) {
                        writer.Write((ushort)0x1F);
                        i += "end>".Length;
                    } else if (text.Substring(i).StartsWith("<pause:")) {
                        int endToken = text.IndexOf('>', i);
                        int numIdx = text.IndexOf(':', i) + 1;
                        int num = int.Parse(text.Substring(numIdx, endToken - numIdx));
                        writer.Write((ushort)0x1E);
                        writer.Write((ushort)num);
                        i = endToken + 1; // includes new line
                    }
                } else if (text[i] == '\n') {
                    writer.Write((ushort)0x0D);
                } else {
                    writer.Write(text[i], Encoding.GetEncoding("utf-16"));
                }
            }

            // It's not from the original game but I want to put it :)
            writer.Write((ushort)0x00);
        }
    }
}
