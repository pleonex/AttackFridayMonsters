//  Copyright (c) 2019 SceneGate Team
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
    using AttackFridayMonsters.Formats.Text.Layout;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;
    using Yarhl.FileFormat;
    using Yarhl.IO;

    public class Clyt2Yml : IConverter<Clyt, BinaryFormat>
    {
        public BinaryFormat Convert(Clyt source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            ClytYml ymlClyt = new ClytYml { Layout = source.Layout.Size };

            Stack<Panel> stack = new Stack<Panel>();
            stack.Push(source.RootPanel);
            while (stack.Count > 0) {
                Panel panelClyt = stack.Pop();
                var ymlPanel = new PanelYml {
                    Name = panelClyt.Name,
                    Type = panelClyt.GetType().Name,
                    Position = panelClyt.Translation,
                    Scale = panelClyt.Scale,
                    Size = panelClyt.Size,
                };
                ymlClyt.Panels.Add(ymlPanel);

                foreach (var child in panelClyt.Children.Reverse()) {
                    stack.Push(child);
                }
            }

            string yamlText = new SerializerBuilder()
                .WithNamingConvention(new UnderscoredNamingConvention())
                .Build()
                .Serialize(ymlClyt);

            BinaryFormat binary = new BinaryFormat();
            new TextWriter(binary.Stream).Write(yamlText);

            return binary;
        }
    }
}
