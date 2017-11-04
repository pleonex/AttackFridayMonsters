//
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
    using Mono.Addins;
    using Yarhl.IO;
    using Yarhl.FileFormat;
    using Yarhl.Media.Text;

    [Extension]
    public class EpisodeSettingsToPo : IConverter<BinaryFormat, Po>
    {
        public Po Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            DataReader reader = new DataReader(source.Stream) {
                DefaultEncoding = Encoding.GetEncoding("utf-16")
            };

            Po po = new Po {
                Header = new PoHeader("AttackFridayMonster Translation", "benito356@gmail.com")
            };

            while (!source.Stream.EndOfStream) {
                uint id = reader.ReadUInt32();
                string text = reader.ReadString(0x50).Replace("\0", "");
                source.Stream.Seek(0x6C, SeekMode.Current); // Unknown

                po.Add(new PoEntry(text) { Context = id.ToString("X8") });
            }

            return po;
        }
    }
}
