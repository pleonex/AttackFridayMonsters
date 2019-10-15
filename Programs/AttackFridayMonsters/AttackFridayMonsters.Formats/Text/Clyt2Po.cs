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
    using Yarhl.FileFormat;
    using Yarhl.Media.Text;

    public class Clyt2Po : IConverter<Clyt, Po>
    {
        public Po Convert(Clyt source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Po po = new Po {
                Header = new PoHeader(
                    "Attack of the Friday Monsters Translation",
                    "benito356@gmail.com",
                    "es-ES"),
            };

            Stack<Panel> stack = new Stack<Panel>();
            stack.Push(source.RootPanel);
            while (stack.Count > 0) {
                Panel panel = stack.Pop();
                if (panel is TextSection text && !string.IsNullOrEmpty(text.Text)) {
                    PoEntry entry = new PoEntry {
                        Context = text.Name,
                        Original = text.Text,
                    };

                    po.Add(entry);
                }

                foreach (var child in panel.Children.Reverse()) {
                    stack.Push(child);
                }
            }

            return po;
        }
    }
}
