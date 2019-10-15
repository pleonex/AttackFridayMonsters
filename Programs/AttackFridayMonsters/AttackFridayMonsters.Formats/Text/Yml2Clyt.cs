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

    public class Yml2Clyt : IConverter<Clyt, Clyt>, IInitializer<BinaryFormat>
    {
        string importedYml;

        public void Initialize(BinaryFormat yml)
        {
            importedYml = new TextReader(yml.Stream).ReadToEnd();
        }

        public Clyt Convert(Clyt source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(importedYml))
                throw new InvalidOperationException("YML file doesn't exist or is empty.");

            ClytYml yml = new DeserializerBuilder()
                .WithNamingConvention(new UnderscoredNamingConvention())
                .Build()
                .Deserialize<ClytYml>(importedYml);

            source.Layout.Size = yml.Layout;

            Stack<Panel> stack = new Stack<Panel>();
            stack.Push(source.RootPanel);
            while (stack.Count > 0) {
                Panel panelClyt = stack.Pop();
                foreach (var child in panelClyt.Children.Reverse()) {
                    stack.Push(child);
                }

                // Search and replace content
                PanelYml panelYml = yml.Panels
                    .First(x => x.Name == panelClyt.Name);

                panelClyt.Translation = panelYml.Position;
                panelClyt.Scale = panelYml.Scale;
                panelClyt.Size = panelYml.Size;
            }

            return source;
        }
    }
}
