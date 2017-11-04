//
//  Ofs3.cs
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
namespace AttackFridayMonsters.Formats.Container
{
    using System;
    using Mono.Addins;
    using Yarhl.IO;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;

    [Extension]
    public class Ofs3ToBinaryConverter : IConverter<BinaryFormat, NodeContainerFormat>
    {
        public NodeContainerFormat Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            NodeContainerFormat container = new NodeContainerFormat();
            DataReader reader = new DataReader(source.Stream);

            // Header
            string magicStamp = reader.ReadString(4);
            if (magicStamp != "OFS3")
                throw new FormatException("Invalid magic stamp");

            uint headerSize = reader.ReadUInt32();
            ushort fatType = reader.ReadUInt16();
            ushort padding = reader.ReadUInt16();
            reader.ReadUInt32(); // Data size

            // FAT
            uint numFiles = reader.ReadUInt32();
            for (int i = 0; i < numFiles; i++) {
                DataStream fileStream = new DataStream(
                    source.Stream,
                    headerSize + reader.ReadUInt32(),
                    reader.ReadUInt32());

                string filename = "File" + i + ".bin";
                if (fatType == 2) {
                    source.Stream.PushToPosition(
                        headerSize + reader.ReadUInt32(),
                        SeekMode.Start);
                    filename = reader.ReadString();
                    source.Stream.PopPosition();
                }

                container.Root.Add(new Node(filename, new BinaryFormat(fileStream)));
            }

            return container;
        }
    }
}
