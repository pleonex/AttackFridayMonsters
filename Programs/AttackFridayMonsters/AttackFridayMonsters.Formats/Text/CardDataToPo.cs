//
//  CardDataToPo.cs
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
    using Mono.Addins;
    using Yarhl.IO;
    using Yarhl.Media.Text;
    using Yarhl.FileFormat;

    [Extension]
    public class CardDataToPo :
        IConverter<BinaryFormat, Po>,
        IConverter<Po, BinaryFormat>
    {
        public CardDataToPo(int fileId)
        {
            FileId = fileId;
        }

        public int FileId {
            get;
            private set;
        }

        public BinaryFormat Convert(Po source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            BinaryFormat binary = new BinaryFormat();
            DataWriter writer = new DataWriter(binary.Stream) {
                DefaultEncoding = Encoding.GetEncoding("utf-16")
            };

            int textSize = FileId == 0 ? 0x140 : 0x280;
            int numBlocks = FileId == 0 ? 80 : source.Entries.Count;
            int entriesPerBlock = FileId == 0 ? 5 : 6;
            bool hasId = FileId == 25;

            for (int i = 0; i < numBlocks; i++) {
                if (hasId && i % entriesPerBlock == 0) {
                    writer.Write((i / entriesPerBlock) + 1);
                    writer.WriteTimes(0x00, 0x0C);
                }

                string text = null;
                if (i < source.Entries.Count) {
                    text = string.IsNullOrEmpty(source.Entries[i].Translated) ?
                                 source.Entries[i].Original :
                                 source.Entries[i].Translated;
                    if (FileId == 25)
                        text = text.Replace("\n", "▼");
                }
                writer.Write(text ?? string.Empty, textSize);
            }

            if (FileId == 0)
                writer.Write((byte)0x00); // don't ask why...

            return binary;
        }

        public Po Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (FileId != 0 && FileId != 25)
                throw new NotSupportedException("File ID not supported");

            DataReader reader = new DataReader(source.Stream) {
                DefaultEncoding = Encoding.GetEncoding("utf-16")
            };

            Po po = new Po {
                Header = new PoHeader(
                    "Attack of Friday Monsters translation",
                    "benito356@gmail.com")
            };

            int textSize = FileId == 0 ? 0x140 : 0x280;
            int numBlocks = FileId == 0 ? 5 : 6;
            bool hasId = FileId == 25;
            string[] cardInfo = { "Glim name", "Name", "Length", "Weight", "Description" };

            int textId = 0;
            int blockId = 0;
            while (source.Stream.Position + textSize <= source.Stream.Length) {
                if (textId % numBlocks == 0) {
                    if (FileId == 0) {
                        blockId++;
                    } else {
                        blockId = reader.ReadInt32();
                        source.Stream.Seek(0x0C, SeekMode.Current);
                    }
                }

                string text = reader.ReadString(textSize)
                                    .Replace("\0", "").Replace("▼", "\n");
                if (!string.IsNullOrEmpty(text)) {
                    int subblock = textId % numBlocks;
                    PoEntry entry = new PoEntry(text) {
                        Context = $"b:{blockId}|s:{subblock}"};
                    if (FileId == 0)
                        entry.ExtractedComments = $"[{blockId}] {cardInfo[subblock]}";
                    po.Add(entry);
                }

                textId++;
            }

            return po;
        }
    }
}
