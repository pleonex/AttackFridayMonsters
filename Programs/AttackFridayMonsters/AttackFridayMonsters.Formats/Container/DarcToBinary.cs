//
//  DarcToBinaryConverter.cs
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
    using System.Text;
    using Yarhl.IO;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;

    public class DarcToBinary : IConverter<BinaryFormat, NodeContainerFormat>
    {
        public NodeContainerFormat Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            NodeContainerFormat container = new NodeContainerFormat();
            DataReader reader = new DataReader(source.Stream) {
                DefaultEncoding = Encoding.GetEncoding("utf-16")
            };

            // Header
            if (reader.ReadString(4, Encoding.ASCII) != "darc")
                throw new FormatException("Invalid magic stamp");
            if (reader.ReadUInt16() != 0xFEFF)
                throw new FormatException("Unknown endianness"); // big endian
            uint headerSize = reader.ReadUInt32();
            if (reader.ReadUInt16() != 0x0100)
                throw new FormatException("Unknown version");
            reader.ReadUInt32(); // file size

            uint fatOffset = reader.ReadUInt32();
            // we are skipping "fat size" and "data offset"
            source.Stream.Position = fatOffset;

            // We expect that the first node is a root node
            // we get the lastId from the root to get the nameTableOffset
            source.Stream.PushToPosition(8, SeekMode.Current);
            uint nameTableOffset = fatOffset + (reader.ReadUInt32() * 0x0C);
            source.Stream.PopPosition();

            Node current = container.Root;
            current.Tags["darc.lastId"] = 1;  // only one entry / root

            int currentId = 0;
            do {
                if (currentId >= current.Tags["darc.lastId"]) {
                    current = current.Parent;
                    continue;
                }

                // Read the entry
                source.Stream.PushToPosition(nameTableOffset + reader.ReadInt24(), SeekMode.Start);
                string name = reader.ReadString();
                if (string.IsNullOrEmpty(name))
                    name = "Entry" + currentId;
                source.Stream.PopPosition();

                bool isFolder = reader.ReadByte() == 1;
                uint offset = reader.ReadUInt32();
                uint size = reader.ReadUInt32();

                Node node = new Node(name);
                current.Add(node);

                if (isFolder) {
                    node.Tags["darc.lastId"] = size;
                    current = node;
                } else {
                    node.Format = new BinaryFormat(source.Stream, offset, size);
                }

                currentId++;
            } while (current != container.Root);

            return container;
        }
    }
}
