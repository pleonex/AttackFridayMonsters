//  BinaryStrings2Po.cs
//
//  Copyright (c) 2020 Benito Palacios Sánchez
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
namespace AttackFridayMonsters.Formats.Text.Code
{
    using System;
    using System.Linq;
    using System.Text;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;
    using Yarhl.FileFormat;
    using Yarhl.IO;
    using Yarhl.Media.Text;

    public sealed class BinaryStrings2Po :
        IConverter<BinaryFormat, Po>, IInitializer<DataStream>
    {
        StringDefinitionBlock block;

        public void Initialize(DataStream parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            string yaml = new TextReader(parameters).ReadToEnd();
            block = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build()
                .Deserialize<StringDefinitionBlock>(yaml);
        }

        public Po Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (block == null)
                throw new InvalidOperationException("Converter not initialized");

            Po po = new Po {
                Header = new PoHeader(
                    "Attack of Friday Monsters translation",
                    "benito356@gmail.com",
                    "es-ES"),
            };

            DataReader reader = new DataReader(source.Stream);
            foreach (var definition in block.Definitions) {
                source.Stream.Position = definition.Address - block.Offset[0].Ram;
                var encoding = Encoding.GetEncoding(definition.Encoding);
                string text = reader.ReadString(definition.Size, encoding).Replace("\0", string.Empty);

                string pointers = string.Join(",", definition.Pointers.Select(p => $"0x{p:X}"));

                var entry = new PoEntry {
                    Original = text,
                    Context = $"0x{definition.Address:X8}",
                    Flags = "c-format",
                    Reference = $"0x{definition.Address:X}:{definition.Size}:{definition.Encoding}:{pointers}",
                };
                po.Add(entry);
            }

            return po;
        }
    }
}
