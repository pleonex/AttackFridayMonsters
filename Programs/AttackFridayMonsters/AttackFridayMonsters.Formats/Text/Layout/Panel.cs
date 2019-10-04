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
namespace AttackFridayMonsters.Formats.Text.Layout
{
    using System.Collections.ObjectModel;

    public class Panel
    {
        public PanelFlags Flags { get; set; }

        public byte Origin { get; set; }

        public byte Alpha { get; set; }

        public PanelMagnificationFlags MagnificationFlags { get; set; }

        public string Name { get; set; }

        public Vector3 Translation { get; set; }

        public Vector3 Rotation { get; set; }

        public Vector2 Scale { get; set; }

        public Vector2 Size { get; set; }

        public Panel Parent { get; set; }

        public Collection<Panel> Children { get; } = new Collection<Panel>();
    }
}
