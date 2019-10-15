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
    public class Picture : Panel
    {
        public uint TopLeftVertexColor { get; set; }

        public uint TopRightVertexColor { get; set; }

        public uint BottomLeftVertexColor { get; set; }

        public uint BottomRightVertexColor { get; set; }

        public int MaterialIndex { get; set; }

        public Vector2[] TopLeftVertexCoords { get; set; }

        public Vector2[] TopRightVertexCoords { get; set; }

        public Vector2[] BottomLeftVertexCoords { get; set; }

        public Vector2[] BottomRightVertexCoords { get; set; }
    }
}
