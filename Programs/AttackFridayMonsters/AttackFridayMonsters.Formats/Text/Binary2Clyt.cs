//  Binary2Clyt.cs
//
//  Copyright (c) 2019 SceneGate
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

    public class Binary2Clyt : IConverter<BinaryFormat, Clyt>
    {
        const string Id = "CLYT";
        const ushort Endianness = 0xFEFF;
        const ushort HeaderSize = 0x14;
        const uint SupportedVersion = 0x02020000; // 2.2.0.0

        DataReader reader;
        Clyt clyt;
        uint numSections;

        public Clyt Convert(BinaryFormat binary)
        {
            if (binary == null)
                throw new ArgumentNullException(nameof(BinaryFormat));

            reader = new DataReader(binary.Stream);
            clyt = new Clyt();

            ReadAndValidateHeader();
            for (int i = 0; i < numSections; i++) {
                ReadSection();
            }

            return clyt;
        }

        void ReadAndValidateHeader()
        {
            reader.Stream.Position = 0;
            if (reader.ReadString(4) != Id)
                throw new FormatException("Invalid header ID");

            if (reader.ReadUInt16() != Endianness)
                throw new FormatException("Unexpected endianness");

            if (reader.ReadUInt16() != HeaderSize)
                throw new FormatException("Unexpected header size");

            if (reader.ReadUInt32() != SupportedVersion)
                throw new NotSupportedException("Unsupported version");

            if (reader.ReadUInt32() != reader.Stream.Length)
                throw new FormatException("Unexpected file size");

            numSections = reader.ReadUInt32();
        }

        void ReadSection()
        {
            reader.Stream.PushCurrentPosition();
            string sectionId = reader.ReadString(4);
            uint size = reader.ReadUInt32();

            switch (sectionId) {
                case "lyt1":
                case "fnl1":
                case "mat1":
                case "txl1":
                case "grs1":
                    // Console.WriteLine(
                    //     $"WARNING: Skipping unsupported section: {sectionId}");
                    break;

                case "txt1":
                    ReadText();
                    break;

                default:
                    // Console.WriteLine(
                    //     $"WARNING: Skipping unknown section: {sectionId}");
                    break;
            }

            reader.Stream.PopPosition();
            reader.Stream.Position += size;
        }

        void ReadText()
        {
            long sectionOffset = reader.Stream.Position - 8;

            reader.Stream.Position = sectionOffset + 8;
            byte unk9 = reader.ReadByte();
            Console.WriteLine($"{nameof(unk9)}: {unk9:X2}");

            reader.Stream.Position = sectionOffset + 9;
            byte unk1 = reader.ReadByte();
            Console.WriteLine($"{nameof(unk1)}: {unk1:X2}");

            reader.Stream.Position = sectionOffset + 0xA;
            byte unk8 = reader.ReadByte();
            Console.WriteLine($"{nameof(unk8)}: {unk8:X2}");

            reader.Stream.Position = sectionOffset + 0xC;
            string unk2 = reader.ReadString();
            Console.WriteLine($"{nameof(unk2)}: {unk2}");

            reader.Stream.Position = sectionOffset + 0x1C;
            string textName = reader.ReadString();
            Console.WriteLine($"{nameof(textName)}: {textName}");

            reader.Stream.Position = sectionOffset + 0x24;
            float[] unk24 = new float[3];
            unk24[0] = reader.ReadSingle();
            unk24[1] = reader.ReadSingle();
            unk24[2] = reader.ReadSingle();
            Console.WriteLine($"{nameof(unk24)}[0]: {unk24[0]}");
            Console.WriteLine($"{nameof(unk24)}[1]: {unk24[1]}");
            Console.WriteLine($"{nameof(unk24)}[2]: {unk24[2]}");

            reader.Stream.Position = sectionOffset + 0x30;
            float[] unk30 = new float[3];
            unk30[0] = reader.ReadSingle();
            unk30[1] = reader.ReadSingle();
            unk30[2] = reader.ReadSingle();
            Console.WriteLine($"{nameof(unk30)}[0]: {unk30[0]}");
            Console.WriteLine($"{nameof(unk30)}[1]: {unk30[1]}");
            Console.WriteLine($"{nameof(unk30)}[2]: {unk30[2]}");

            reader.Stream.Position = sectionOffset + 0x3C;
            float[] unk3C = new float[2];
            unk3C[0] = reader.ReadSingle();
            unk3C[1] = reader.ReadSingle();
            Console.WriteLine($"{nameof(unk3C)}[0]: {unk3C[0]}");
            Console.WriteLine($"{nameof(unk3C)}[1]: {unk3C[1]}");

            reader.Stream.Position = sectionOffset + 0x44;
            float[] unk44 = new float[2];
            unk44[0] = reader.ReadSingle();
            unk44[1] = reader.ReadSingle();
            Console.WriteLine($"{nameof(unk44)}[0]: {unk44[0]}");
            Console.WriteLine($"{nameof(unk44)}[1]: {unk44[1]}");

            reader.Stream.Position = sectionOffset + 0x4C;
            ushort unk10 = reader.ReadUInt16();
            Console.WriteLine($"{nameof(unk10)}: {unk10:X4}");

            reader.Stream.Position = sectionOffset + 0x54;
            byte unk54 = reader.ReadByte();
            Console.WriteLine($"{nameof(unk54)}: {unk54:X2}");

            reader.Stream.Position = sectionOffset + 0x55;
            byte unk55 = reader.ReadByte();
            Console.WriteLine($"{nameof(unk55)}: {unk55:X2}");

            reader.Stream.Position = sectionOffset + 0x5C;
            uint[] unk5C = new uint[2];
            unk5C[0] = reader.ReadUInt32();
            unk5C[1] = reader.ReadUInt32();
            Console.WriteLine($"{nameof(unk5C)}[0]: {unk5C[0]:X8}");
            Console.WriteLine($"{nameof(unk5C)}[1]: {unk5C[1]:X8}");

            reader.Stream.Position = sectionOffset + 0x64;
            float[] unk64 = new float[2];
            unk64[0] = reader.ReadSingle();
            unk64[1] = reader.ReadSingle();
            Console.WriteLine($"{nameof(unk64)}[0]: {unk64[0]}");
            Console.WriteLine($"{nameof(unk64)}[1]: {unk64[1]}");

            reader.Stream.Position = sectionOffset + 0x6C;
            float unk6C = reader.ReadSingle();
            Console.WriteLine($"{nameof(unk6C)}: {unk6C}");

            reader.Stream.Position = sectionOffset + 0x70;
            byte unk70 = reader.ReadByte();
            Console.WriteLine($"{nameof(unk70)}: {unk70:X8}");

            reader.Stream.Position = sectionOffset + 0x52;
            ushort fontIdx = reader.ReadUInt16();
            Console.WriteLine($"{nameof(fontIdx)}: {fontIdx}");

            reader.Stream.Position = sectionOffset + 0x50;
            ushort matOffset = reader.ReadUInt16();
            Console.WriteLine($"{nameof(matOffset)}: {matOffset:X4}");

            reader.Stream.Position = sectionOffset + 0x4E;
            ushort textSize = reader.ReadUInt16();

            reader.Stream.Position = sectionOffset + 0x58;
            uint textOffset = reader.ReadUInt32();

            reader.Stream.Position = sectionOffset + textOffset;
            string text = reader.ReadString(Encoding.Unicode);
            Console.WriteLine($"Text: {text}");

            clyt.Messages.Add(text);
        }
    }
}