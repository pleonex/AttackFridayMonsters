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
namespace AttackFridayMonsters.Formats.Text.Layout
{
    using System.Collections.ObjectModel;
    using Yarhl.FileFormat;

    /// <summary>
    /// CTR Layout format.
    /// </summary>
    public class Clyt : IFormat
    {
        public LayoutDefinition Layout { get; set; }

        public Collection<Material> Materials { get; } = new Collection<Material>();

        public Group RootGroup { get; set; }

        public Collection<string> Textures { get; } = new Collection<string>();

        public Collection<string> Fonts { get; } = new Collection<string>();

        public Panel RootPanel { get; set; }
    }
}
