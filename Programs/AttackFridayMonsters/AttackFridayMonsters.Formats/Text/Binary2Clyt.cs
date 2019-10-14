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
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using AttackFridayMonsters.Formats.Text.Layout;
    using Yarhl.FileFormat;
    using Yarhl.IO;

    /// <summary>
    /// Binary (BCLYT) to layout format CLYT converter.
    /// </summary>
    /// <remarks>
    /// <p>Based on assembly research and information from:
    /// https://www.3dbrew.org/wiki/CLYT_format</p>
    /// </remarks>
    public class Binary2Clyt : IConverter<BinaryFormat, Clyt>
    {
        const string Id = "CLYT";
        const ushort Endianness = 0xFEFF;
        const ushort HeaderSize = 0x14;
        const uint SupportedVersion = 0x02020000; // 2.2.0.0

        readonly Dictionary<string, Action> sectionReadFnc;
        DataReader reader;
        Clyt clyt;
        Panel currentPanel;
        uint sectionSize;

        public Binary2Clyt()
        {
           sectionReadFnc = new Dictionary<string, Action> {
                { "lyt1", ReadLayout },
                { "txl1", ReadTextureLoad },
                { "mat1", ReadMaterial },
                { "fnl1", ReadFontLoad },
                { "pas1", ReadPanelStart },
                { "pae1", ReadPanelEnd },
                { "pan1", ReadPanel },
                { "txt1", ReadText },
                { "pic1", ReadPicture },
                { "wnd1", ReadWindow },
                { "grp1", ReadGroup },
            };
        }

        public Clyt Convert(BinaryFormat binary)
        {
            if (binary == null)
                throw new ArgumentNullException(nameof(BinaryFormat));

            reader = new DataReader(binary.Stream);
            clyt = new Clyt();

            uint numSections = ReadAndValidateHeader();
            while (numSections > 0) {
                ReadSection();
                numSections--;
            }

            return clyt;
        }

        uint ReadAndValidateHeader()
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

            uint numSections = reader.ReadUInt32();
            return numSections;
        }

        void ReadSection()
        {
            reader.Stream.PushCurrentPosition();
            string sectionId = reader.ReadString(4);
            sectionSize = reader.ReadUInt32();

            if (sectionReadFnc.ContainsKey(sectionId)) {
                sectionReadFnc[sectionId]?.Invoke();
            }

            reader.Stream.PopPosition();
            reader.Stream.Position += sectionSize;
        }

        void ReadLayout()
        {
            clyt.Layout = new LayoutDefinition {
                Origin = (LayoutOrigin)reader.ReadUInt32(),
                Size = new Size(reader.ReadSingle(), reader.ReadSingle())
            };
        }

        void ReadMaterial()
        {
            if (clyt.Materials.Count > 0) {
                throw new FormatException("Expecting just one mat1 section");
            }

            long baseOffset = reader.Stream.Position - 8;
            int numMaterials = reader.ReadInt32();

            while (numMaterials > 0) {
                // Read from table and jump to material
                long offset = baseOffset + reader.ReadUInt32();
                reader.Stream.PushToPosition(offset);

                var material = new Material();
                material.Name = reader.ReadString(0x14).Replace("\0", string.Empty);

                for (int i = 0; i < material.TevConstantColors.Length; i++) {
                    // TODO: Convert to RGBA
                    material.TevConstantColors[i] = reader.ReadUInt32();
                }

                uint flags = reader.ReadUInt32();
                uint numTexMap = flags & 0x3;
                uint numTexMatrix = (flags >> 2) & 0x3;
                uint numTexCoordGen = (flags >> 4) & 0x3;
                uint numTevStage = (flags >> 6) & 0x7;
                uint hasAlphaCompare = (flags >> 9) & 0x01;
                uint hasBlendMode = (flags >> 10) & 0x01;
                material.UseTextureOnly = ((flags >> 11) & 0x01) == 1;
                uint separateBlendMode = (flags >> 12) & 0x01;
                uint hasIndirectParam = (flags >> 13) & 0x01;
                uint numProjTexGenParam = (flags >> 14) & 0x03;
                uint hasFontShadowParam = (flags >> 16) & 0x01;

                uint texMapOffset = 0x34;
                uint texMatrixOffset = texMapOffset + (numTexMap * 4);
                uint texCoordGenOffset = texMatrixOffset + (numTexMatrix * 0x14);
                uint tevStageOffset = texCoordGenOffset + (numTexCoordGen * 4);
                uint alphaCompareOffset = tevStageOffset + (numTevStage * 0xC);
                uint blendModeOffset = alphaCompareOffset + 0x8;

                reader.Stream.Seek(offset + texMapOffset);
                for (int i = 0; i < numTexMap; i++) {
                    var entry = new TextureMapEntry();
                    entry.Index = reader.ReadUInt16();

                    byte flag1 = reader.ReadByte();
                    byte flag2 = reader.ReadByte();
                    entry.WrapS = (WrapMode)(flag1 & 0x3);
                    entry.WrapT = (WrapMode)(flag2 & 0x3);
                    entry.MinFilter = (TextureFilter)((flag1 >> 2) & 0x3);
                    entry.MagFilter = (TextureFilter)((flag2 >> 2) & 0x3);

                    material.TexMapEntries.Add(entry);
                }

                reader.Stream.Seek(offset + texMatrixOffset);
                for (int i = 0; i < numTexMatrix; i++) {
                    var entry = new TextureMatrixEntry();
                    entry.Translation = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    entry.Rotation = reader.ReadSingle();
                    entry.Scale = new Vector2(reader.ReadSingle(), reader.ReadSingle());

                    material.TexMatrixEntries.Add(entry);
                }

                reader.Stream.Seek(offset + texCoordGenOffset);
                for (int i = 0; i < numTexCoordGen; i++) {
                    material.TextureCoordGen.Add(reader.ReadSingle());
                }

                // TODO: Find a bclyt with the rest of sections

                clyt.Materials.Add(material);
                reader.Stream.PopPosition();
                numMaterials--;
            }
        }

        void ReadPanel()
        {
            Panel panel = new Panel();
            ReadPanel(panel);

            if (currentPanel == null) {
                clyt.RootPanel = panel;
            } else {
                panel.Parent = currentPanel;
                currentPanel.Children.Add(panel);
            }
        }

        private void ReadPanelStart()
        {
            // Nothing to read, just layout marker
            if (currentPanel == null) {
                currentPanel = clyt.RootPanel;
            } else {
                currentPanel = currentPanel.Children.Last();
            }
        }

        private void ReadPanelEnd()
        {
            // Nothing to read, just layout marker
            currentPanel = currentPanel.Parent;
        }

        void ReadPanel(Panel panel)
        {
            panel.Flags = (PanelFlags)reader.ReadByte();
            panel.Origin = reader.ReadByte();
            panel.Alpha = reader.ReadByte();
            panel.MagnificationFlags = (PanelMagnificationFlags)reader.ReadByte();

            panel.Name = reader.ReadString(0x18).Replace("\0", string.Empty);

            panel.Translation = new Vector3 {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle(),
            };

            panel.Rotation = new Vector3 {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle(),
            };

            panel.Scale = new Vector2 {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
            };

            panel.Size = new Size {
                Width = reader.ReadSingle(),
                Height = reader.ReadSingle(),
            };
        }

        void ReadGroup()
        {
            if (clyt.Group != null)
                throw new FormatException("Duplicated group");

            clyt.Group = reader.ReadString();
        }

        void ReadTextureLoad()
        {
            int num = reader.ReadInt32();
            long baseOffset = reader.Stream.Position;

            for (int i = 0; i < num; i++) {
                uint offset = reader.ReadUInt32();
                reader.Stream.RunInPosition(
                    () => clyt.Textures.Add(reader.ReadString()),
                    baseOffset + offset);
            }
        }

        void ReadFontLoad()
        {
            int num = reader.ReadInt32();
            long baseOffset = reader.Stream.Position;

            for (int i = 0; i < num; i++) {
                uint offset = reader.ReadUInt32();
                reader.Stream.RunInPosition(
                    () => clyt.Fonts.Add(reader.ReadString()),
                    baseOffset + offset);
            }
        }

        void ReadWindow()
        {
            Window window = new Window();
            ReadPanel(window);
            window.Parent = currentPanel;
            currentPanel.Children.Add(window);

            // TODO: Parse rest of the section
            int unknownSize = (int)sectionSize - 0x44 - 0x08;
            window.Unknown = reader.ReadBytes(unknownSize);
        }

        void ReadPicture()
        {
            Picture picture = new Picture();
            ReadPanel(picture);
            picture.Parent = currentPanel;
            currentPanel.Children.Add(picture);

            picture.TopLeftVertexColor = reader.ReadUInt32();
            picture.TopRightVertexColor = reader.ReadUInt32();
            picture.BottomLeftVertexColor = reader.ReadUInt32();
            picture.BottomRightVertexColor = reader.ReadUInt32();
            picture.MaterialIndex = reader.ReadUInt16();

            int numCoords = reader.ReadInt16();
            picture.TopLeftVertexCoords = new Vector2[numCoords];
            picture.TopRightVertexCoords = new Vector2[numCoords];
            picture.BottomLeftVertexCoords = new Vector2[numCoords];
            picture.BottomRightVertexCoords = new Vector2[numCoords];
            for (int i = 0; i < numCoords; i++) {
                picture.TopLeftVertexCoords[i] = new Vector2(
                    reader.ReadSingle(),
                    reader.ReadSingle());
                picture.TopRightVertexCoords[i] = new Vector2(
                    reader.ReadSingle(),
                    reader.ReadSingle());
                picture.BottomLeftVertexCoords[i] = new Vector2(
                    reader.ReadSingle(),
                    reader.ReadSingle());
                picture.BottomRightVertexCoords[i] = new Vector2(
                    reader.ReadSingle(),
                    reader.ReadSingle());
            }
        }

        void ReadText()
        {
            long sectionOffset = reader.Stream.Position - 8;
            TextSection section = new TextSection();
            ReadPanel(section);

            section.Unknown4C = reader.ReadUInt16();

            ushort textSize = reader.ReadUInt16();
            section.MaterialIndex = reader.ReadUInt16();
            section.FontIndex = reader.ReadUInt16();

            section.Unknown54 = reader.ReadByte();
            section.Unknown55 = reader.ReadByte();
            reader.ReadUInt16(); // reserved?

            uint textOffset = reader.ReadUInt32();

            section.Unknown5C = new int[2];
            section.Unknown5C[0] = reader.ReadInt32();
            section.Unknown5C[1] = reader.ReadInt32();

            section.Unknown64 = new Vector2(
                reader.ReadSingle(),
                reader.ReadSingle());

            section.Unknown6C = reader.ReadSingle();

            section.Unknown70 = reader.ReadSingle();

            reader.Stream.Position = sectionOffset + textOffset;
            section.Text = reader.ReadString(textSize, Encoding.Unicode)
                .Replace("\0", string.Empty);

            section.Parent = currentPanel;
            currentPanel.Children.Add(section);
        }
    }
}
