//  Copyright (c) 2020 GradienWords
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
namespace Patcher.Patching
{
    using System;
    using System.Threading.Tasks;
    using SceneGate.Lemon.Containers.Converters;
    using Yarhl.IO;

    public class GameExporterLayeredFs
    {
        static readonly string HomePath = System.Environment.GetEnvironmentVariable("HOME");
        static readonly string CitraPathWindows = @$"{HomePath}\AppData\Roaming\Citra\load\mods";
        static readonly string CitraPathUnix = $"{HomePath}/.local/share/citra-emu/load/mods";
        static readonly string CitraPath = (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
            ? CitraPathWindows
            : CitraPathUnix;

        public GameExporterLayeredFs(GameNode game) => Game = game;

        public GameNode Game { get; }

        public event EventHandler<double> ProgressChanged;

        public async Task ExportToDirectoryAsync(string output)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            ProgressChanged?.Invoke(this, 1);
        }

        public async Task ExportToCitraAsync()
        {
            await Task.Delay(1000).ConfigureAwait(false);
            ProgressChanged?.Invoke(this, 1);
        }

        private void Unpack()
        {
            var programNode = Game.Root.Children["content"].Children["program"];
            if (programNode.Format is BinaryFormat) {
                Logger.Log("Converting binary NCCH");
                programNode.TransformWith<Binary2Ncch>();
                programNode.Children["rom"].TransformWith<BinaryIvfc2NodeContainer>();
                programNode.Children["system"].TransformWith<BinaryExeFs2NodeContainer>();
            }
        }
    }
}