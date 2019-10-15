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

    public class Material
    {
        public string Name { get; set; }

        public uint[] TevConstantColors { get; } = new uint[7];

        public bool UseTextureOnly { get; set; }

        public Collection<TextureMapEntry> TexMapEntries { get; } = new Collection<TextureMapEntry>();

        public Collection<TextureMatrixEntry> TexMatrixEntries { get; } = new Collection<TextureMatrixEntry>();

        public Collection<float> TextureCoordGen { get; } = new Collection<float>();
    }
}
