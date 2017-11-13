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
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Yarhl.IO;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;

    public class DarcToBinary :
        IConverter<BinaryFormat, NodeContainerFormat>,
        IConverter<NodeContainerFormat, BinaryFormat>
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

        public BinaryFormat Convert(NodeContainerFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            BinaryFormat binary = new BinaryFormat();
            DataWriter writer = new DataWriter(binary.Stream);
            Encoding utf16 = Encoding.GetEncoding("utf-16");

            // header
            writer.Write("darc", false, Encoding.ASCII);
            writer.Write((ushort)0xFEFF);
            writer.Write(0x1C);
            writer.Write((ushort)0x0100);
            writer.Write(0x00);  // Placeholder for file size
            writer.Write(0x1C);
            writer.Write(0x00); // Placeholder for fat size
            writer.Write(0x00); // Placeholder for data offset

            // First iteration, get file id and write names
            DataStream names = new DataStream();
            DataWriter nameWriter = new DataWriter(names);

            Stack<Node> stack = new Stack<Node>();
            stack.Push(source.Root);
            int currentId = 0;
            while (stack.Any()) {
                var node = stack.Pop();

                var name = node == source.Root ? string.Empty : node.Name;
                var nameBin = utf16.GetBytes(name + "\0");
                nameWriter.Write(nameBin);

                node.Tags["darc_name"] = nameBin.Length;
                currentId++;

                Node t = node;
                while (t.Parent != null) {
                    t.Parent.Tags["darc_lastId"] = currentId;
                    t = t.Parent;
                }

                foreach (var child in node.Children.Reverse())
                    stack.Push(child);
            }

            // Write empty FAT
            writer.WriteTimes(0x00, 0x0C * currentId);
            names.WriteTo(binary.Stream);
            names.Dispose();
            uint endFatPos = (uint)binary.Stream.Length;
            writer.WritePadding(0x00, 0x80);

            // Start writing FAT again
            binary.Stream.Position = 0x14;
            writer.Write(endFatPos - 0x1C);
            writer.Write((uint)binary.Stream.Length);

            stack.Clear();
            stack.Push(source.Root);
            ushort nameOffset = 0;
            while(stack.Any()) {
                var node = stack.Pop();
                DataStream fileStream = node.GetFormatAs<BinaryFormat>()?.Stream;

                writer.Write(nameOffset);
                nameOffset += (ushort)node.Tags["darc_name"];

                if (fileStream != null) {
                    writer.Write((ushort)0);
                    writer.Write((uint)binary.Stream.Length);
                    writer.Write((uint)fileStream.Length);

                    // Write file
                    binary.Stream.PushToPosition(0, SeekMode.End);
                    writer.WritePadding(0x00, 0x80);
                    fileStream.WriteTo(binary.Stream);
                    binary.Stream.PopPosition();
                } else {
                    writer.Write((ushort)0x100);
                    writer.Write(0);
                    writer.Write(node.Tags["darc_lastId"]);
                }

                foreach (var child in node.Children.Reverse())
                    stack.Push(child);
            }

            // Update file size place holder
            binary.Stream.Position = 0x0C;
            writer.Write((uint)binary.Stream.Length);

            return binary;
        }
    }
}
