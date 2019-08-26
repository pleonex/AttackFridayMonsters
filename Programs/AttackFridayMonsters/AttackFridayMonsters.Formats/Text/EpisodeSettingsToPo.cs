//  EpisodeSettingsToPo.cs
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

    public class EpisodeSettingsToPo :
        IInitializer<DataStream>,
        IConverter<BinaryFormat, Po>,
        IConverter<Po, BinaryFormat>
    {
        public DataStream Original { get; private set; }

        public void Initialize(DataStream original)
        {
            Original = original;
        }

        public BinaryFormat Convert(Po source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (Original == null)
                throw new ArgumentNullException(nameof(Original));

            BinaryFormat binary = new BinaryFormat();
            DataWriter writer = new DataWriter(binary.Stream) {
                DefaultEncoding = Encoding.GetEncoding("utf-16"),
            };

            Original.Position = 0;
            DataReader reader = new DataReader(Original) {
                DefaultEncoding = Encoding.GetEncoding("utf-16"),
            };

            foreach (var entry in source.Entries) {
                // ID
                writer.Write(reader.ReadUInt32());

                // Text
                reader.Stream.Seek(0x50, SeekMode.Current);
                string text = string.IsNullOrEmpty(entry.Translated) ?
                                    entry.Original : entry.Translated;
                writer.Write(text, 0x50);

                // Unknown
                writer.Write(reader.ReadBytes(0x6C));
            }

            return binary;
        }

        public Po Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            DataReader reader = new DataReader(source.Stream) {
                DefaultEncoding = Encoding.GetEncoding("utf-16"),
            };

            Po po = new Po {
                Header = new PoHeader(
                    "AttackFridayMonster Translation",
                    "benito356@gmail.com",
                    "es-ES"),
            };

            while (!source.Stream.EndOfStream) {
                uint id = reader.ReadUInt32();

                // Japanese version has 0x38 bytes of text and 0x74 of unknown
                string text = reader.ReadString(0x50).Replace("\0", string.Empty);
                source.Stream.Seek(0x6C, SeekMode.Current); // Unknown

                po.Add(new PoEntry(text) { Context = "id:" + id });
            }

            return po;
        }
    }
}
