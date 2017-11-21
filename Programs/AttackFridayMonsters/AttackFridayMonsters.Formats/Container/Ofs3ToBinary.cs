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
    public class Ofs3ToBinary :
        IConverter<BinaryFormat, NodeContainerFormat>,
        IConverter<NodeContainerFormat, BinaryFormat>
    {
        public short Padding { get; set; } = 0x80;

        public BinaryFormat Convert(NodeContainerFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            BinaryFormat binary = new BinaryFormat();
            DataWriter writer = new DataWriter(binary.Stream);

            bool hasNames = !source.Root.Children[0].Name.StartsWith("File", StringComparison.InvariantCulture);
            int numFiles = source.Root.Children.Count;
            int fatEntrySize = hasNames ? 0x0C : 0x08;

            uint headerSize = 0x10;

            // Header
            writer.Write("OFS3", false);
            writer.Write(headerSize); // header size
            writer.Write((ushort)(hasNames ? 2 : 0));
            writer.Write(Padding);
            writer.Write(0x00); // placeholder for data size
            writer.Write(numFiles);

            // FAT
            // write first empty FAT to write names at the same time
            writer.WriteTimes(0x00, fatEntrySize * numFiles);
            writer.WritePadding(0x00, Padding);

            binary.Stream.Seek(0x14, SeekMode.Start);
            for (int i = 0; i < numFiles; i++) {
                // Get file without names in order
                Node child;
                if (hasNames)
                    child = source.Root.Children[i];
                else
                    child = source.Root.Children["File" + i + ".bin"];

                // Fat Entry
                writer.Write((uint)(binary.Stream.Length - headerSize));
                writer.Write((uint)child.GetFormatAs<BinaryFormat>()?.Stream.Length);
                if (hasNames)
                    writer.Write(0x00);

                // File data
                binary.Stream.PushToPosition(0, SeekMode.End);
                child.GetFormatAs<BinaryFormat>()?.Stream.WriteTo(binary.Stream);
                writer.WritePadding(0x00, Padding);
                binary.Stream.PopPosition();
            }

            // Update the file size (without names)
            binary.Stream.Position = 0x0C;
            writer.Write((uint)(binary.Stream.Length - headerSize));

            // If has name, write them and update FAT
            for (int i = 0; hasNames && i < numFiles; i++) {
                Node child = source.Root.Children[i];

                binary.Stream.Seek(0x14 + i * fatEntrySize + 0x08, SeekMode.Start);
                writer.Write((uint)(binary.Stream.Length - headerSize));

                binary.Stream.Seek(0x00, SeekMode.End);
                writer.Write(child.Name);
            }

            binary.Stream.Seek(0, SeekMode.End);
            writer.WritePadding(0x00, Padding);
            return binary;
        }

        public NodeContainerFormat Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            NodeContainerFormat container = new NodeContainerFormat();
            source.Stream.Seek(0, SeekMode.Start);
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
                uint offset = headerSize + reader.ReadUInt32();
                uint size = reader.ReadUInt32();
                DataStream fileStream = new DataStream(source.Stream, offset, size);

                string filename = string.Empty;
                if (fatType == 2) {
                    source.Stream.PushToPosition(
                        headerSize + reader.ReadUInt32(),
                        SeekMode.Start);
                    filename = reader.ReadString();
                    source.Stream.PopPosition();
                } else if (fatType == 0 || fatType == 1) {
                    filename = "File" + i + ".bin";
                } else {
                    throw new FormatException("Unkown FAT type: " + fatType);
                }

                container.Root.Add(new Node(filename, new BinaryFormat(fileStream)));
            }

            return container;
        }
    }
}
