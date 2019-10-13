// Copyright (c) 2019 SceneGate Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
namespace AttackFridayMonsters.Formats.Text
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using AttackFridayMonsters.Formats.Text.Layout;
    using Yarhl.FileFormat;
    using Yarhl.IO;

    /// <summary>
    /// CLYT layout format to binary (BCLYT).
    /// </summary>
    /// <remarks>
    /// <p>Based on assembly research and information from:
    /// https://www.3dbrew.org/wiki/CLYT_format</p>
    /// </remarks>
    public class Clyt2Binary : IConverter<Clyt, BinaryFormat>
    {
        const string Id = "CLYT";
        const ushort Endianness = 0xFEFF;
        const ushort HeaderSize = 0x14;
        const uint Version = 0x02020000; // 2.2.0.0

        readonly Dictionary<string, Action> sectionWriteFnc;
        DataWriter writer;
        int sections;

        public Clyt2Binary()
        {
           sectionWriteFnc = new Dictionary<string, Action> {
                // { "lyt1", ReadLayout },
                // { "txl1", ReadTextureLoad },
                // { "mat1", ReadMaterial },
                // { "fnl1", ReadFontLoad },
                // { "pas1", ReadPanelStart },
                // { "pae1", ReadPanelEnd },
                // { "pan1", ReadPanel },
                // { "txt1", ReadText },
                // { "pic1", ReadPicture },
                // { "wnd1", ReadWindow },
                // { "grp1", ReadGroup },
            };
        }

        public BinaryFormat Convert(Clyt source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var binary = new BinaryFormat();
            writer = new DataWriter(binary.Stream);

            WriteHeader(source);
            WriteSections(source);
            FinishHeader();

            return binary;
        }

        void WriteHeader(Clyt source)
        {
            writer.Write(Id, nullTerminator: false);
            writer.Write(Endianness);
            writer.Write(HeaderSize);
            writer.Write(Version);
            writer.Write(0x00); // placeholder for size
            writer.Write(0x00); // placeholder for number of sections
        }

        void FinishHeader()
        {
            writer.Stream.Position = 0x0C;
            writer.Write((uint)writer.Stream.Length);
            writer.Write(sections);
        }

        void WriteSections(Clyt source)
        {
            WriteSection("lyt1", () => WriteLayout(source.Layout));
            WriteSection("txl1", () => WriteTextures(source.Textures));
            WriteSection("fnl1", () => WriteFonts(source.Fonts));
            WriteSection("mat1", () => WriteMaterials(source.Materials));

            Stack<Panel> stack = new Stack<Panel>();
            stack.Push(source.RootPanel);
            while (stack.Count > 0) {
                Panel panel = stack.Pop();
                if (panel is TextSection text) {
                    WriteSection("txt1", () => WriteTextInfo(text));
                } else if (panel is Picture picture) {
                    WriteSection("pic1", () => WritePictureInfo(picture));
                } else if (panel is Window window) {
                    WriteSection("wnd1", () => WriteWindow(window));
                } else {
                    WriteSection("pan1", () => WritePanel(panel));
                }

                if (panel.Children.Any()) {
                    WriteSection("pas1", () => {});
                    foreach (var child in panel.Children.Reverse()) {
                        stack.Push(child);
                    }

                    WriteSection("pae1", () => {});
                }
            }

            WriteSection("grp1", () => WriteGroup(source.Group));
        }

        void WriteSection(string id, Action writeFnc)
        {
            long initialSize = writer.Stream.Length;
            long initialPos = writer.Stream.Position;

            writer.Write(id, nullTerminator: false);
            writer.Write(0x00); // place holder for size
            writeFnc();
            writer.WritePadding(0x00, 4);

            // Update size
            uint sectionSize = (uint)(writer.Stream.Length - initialSize);
            writer.Stream.Position = initialPos + 0x04;
            writer.Write(initialSize);

            writer.Stream.Position = initialPos + sectionSize;
            sections++;
        }

        void WriteLayout(LayoutDefinition layout)
        {
            writer.Write((uint)layout.Origin);
            writer.Write(layout.Size.Width);
            writer.Write(layout.Size.Height);
        }

        void WriteTextures(Collection<string> textures)
        {
            writer.Write(textures.Count);

            // Pre-initialize offset table so we can write names at the same time
            long tablePos = writer.Stream.Position;
            writer.WriteTimes(0x00, 4 * textures.Count);

            for (int i = 0; i < textures.Count; i++) {
                writer.Stream.RunInPosition(
                    () => writer.Write((uint)(writer.Stream.Length - tablePos)),
                    tablePos + (i * 4));
                writer.Write(textures[i]);
            }
        }

        void WriteFonts(Collection<string> fonts)
        {
            writer.Write(fonts.Count);

            // Pre-initialize offset table so we can write names at the same time
            long tablePos = writer.Stream.Position;
            writer.WriteTimes(0x00, 4 * fonts.Count);

            for (int i = 0; i < fonts.Count; i++) {
                writer.Stream.RunInPosition(
                    () => writer.Write((uint)(writer.Stream.Length - tablePos)),
                    tablePos + (i * 4));
                writer.Write(fonts[i]);
            }
        }

        void WriteMaterials(Collection<Material> materials)
        {
            writer.Write(materials.Count);

            // Pre-initialize offset table so we can write names at the same time
            long tablePos = writer.Stream.Position;
            writer.WriteTimes(0x00, 4 * materials.Count);

            for (int idx = 0; idx < materials.Count; idx++) {
                writer.Stream.RunInPosition(
                    () => writer.Write((uint)(writer.Stream.Length - tablePos)),
                    tablePos + (idx * 4));

                Material mat = materials[idx];
                writer.Write(mat.Name, 0x14);

                for (int j = 0; j < mat.TevConstantColors.Length; j++) {
                    writer.Write(mat.TevConstantColors[j]);
                }

                int flag = 0x00;
                flag |= mat.TexMapEntries.Count;
                flag |= (mat.TexMatrixEntries.Count << 2);
                flag |= (mat.TextureCoordGen.Count << 4);
                // TODO: Find a bclyt with the rest of sections

                foreach (var entry in mat.TexMapEntries) {
                    writer.Write((ushort)entry.Index);

                    int flag1 = (byte)(entry.WrapS);
                    int flag2 = (byte)(entry.WrapT);
                    flag1 |= (byte)(entry.MinFilter) << 2;
                    flag2 |= (byte)(entry.MagFilter) << 2;

                    writer.Write((byte)flag1);
                    writer.Write((byte)flag2);
                }

                foreach (var entry in mat.TexMatrixEntries) {
                    writer.Write(entry.Translation.X);
                    writer.Write(entry.Translation.Y);
                    writer.Write(entry.Rotation);
                    writer.Write(entry.Scale.X);
                    writer.Write(entry.Scale.Y);
                }

                foreach (var coord in mat.TextureCoordGen) {
                    writer.Write(coord);
                }
            }
        }

        void WriteGroup(string group)
        {
            writer.Write(group, 0x10);
            writer.Write(0x00); // TODO: Find a bclyt with a different value
        }

        void WritePanel(Panel panel)
        {
            writer.Write((byte)panel.Flags);
            writer.Write(panel.Origin);
            writer.Write(panel.Alpha);
            writer.Write((byte)panel.MagnificationFlags);

            writer.Write(panel.Name, 0x18);

            writer.Write(panel.Translation.X);
            writer.Write(panel.Translation.Y);
            writer.Write(panel.Translation.Z);

            writer.Write(panel.Rotation.X);
            writer.Write(panel.Rotation.Y);
            writer.Write(panel.Rotation.Z);

            writer.Write(panel.Scale.X);
            writer.Write(panel.Scale.Y);

            writer.Write(panel.Size.Width);
            writer.Write(panel.Size.Height);
        }

        void WriteTextInfo(TextSection textInfo)
        {
            byte[] utf16Text = Encoding.Unicode.GetBytes(textInfo.Text + "\0");

            WritePanel(textInfo);

            writer.Write(textInfo.Unknown4C);
            writer.Write((ushort)utf16Text.Length);
            writer.Write(textInfo.MaterialIndex);
            writer.Write(textInfo.FontIndex);

            writer.Write(textInfo.Unknown54);
            writer.Write(textInfo.Unknown55);
            writer.Write((ushort)0x00); // reserved

            // start text address is always 0x74 because previous fields
            // have a constant size always.
            writer.Write(0x74);

            writer.Write(textInfo.Unknown5C[0]);
            writer.Write(textInfo.Unknown5C[1]);
            writer.Write(textInfo.Unknown64.X);
            writer.Write(textInfo.Unknown64.Y);
            writer.Write(textInfo.Unknown6C);
            writer.Write(textInfo.Unknown70);
            writer.Write((byte)0x00); // reserved
            writer.Write((ushort)0x00); // reserved

            writer.Write(utf16Text);
        }

        void WritePictureInfo(Picture picInfo)
        {
            WritePanel(picInfo);

            writer.Write(picInfo.TopLeftVertexColor);
            writer.Write(picInfo.TopRightVertexColor);
            writer.Write(picInfo.BottomLeftVertexColor);
            writer.Write(picInfo.BottomRightVertexColor);

            writer.Write(picInfo.MaterialIndex);

            int count = picInfo.TopLeftVertexCoords.Length;
            writer.Write((ushort)count);
            for (int i = 0; i < count; i++) {
                writer.Write(picInfo.TopLeftVertexCoords[i].X);
                writer.Write(picInfo.TopLeftVertexCoords[i].Y);
                writer.Write(picInfo.TopRightVertexCoords[i].X);
                writer.Write(picInfo.TopRightVertexCoords[i].Y);
                writer.Write(picInfo.BottomLeftVertexCoords[i].X);
                writer.Write(picInfo.BottomLeftVertexCoords[i].Y);
                writer.Write(picInfo.BottomRightVertexCoords[i].X);
                writer.Write(picInfo.BottomRightVertexCoords[i].Y);
            }
        }

        void WriteWindow(Window window)
        {
            WritePanel(window);
            writer.Write(window.Unknown);
        }
    }
}
