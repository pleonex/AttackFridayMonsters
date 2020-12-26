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
    using System.IO;
    using System.Threading.Tasks;
    using Xdelta;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    public class GamePatcher
    {
        private readonly GamePatch patch;

        public GamePatcher(GamePatch patch) => this.patch = patch;

        public event ProgressChangedHandler ProgressChanged;

        public async Task PatchAsync(GameNode game)
        {
            if (game.PatchInfo == null) {
                Logger.Log("PatchInfo is null");
                throw new NotSupportedException("No patch available");
            }

            var programNode = game.Root.Children["content"].Children["program"];
            if (programNode.Format is not BinaryFormat) {
                throw new FormatException("NCCH is not binary");
            }

            await Task.Run(() => Patch(programNode, game.PatchInfo.ResourcePath))
                .ConfigureAwait(false);
        }

        private void Patch(Node source, string patchResource)
        {
            // In memory, no need to dispose as we transfer ownership later to the node
            var target = new BinaryFormat();

            var assembly = typeof(GamePatcher).Assembly;
            using Stream patch = assembly.GetManifestResourceStream(patchResource);

            using var decoder = new Decoder(source.Stream, patch, target.Stream);
            decoder.ProgressChanged += progress => {
                Logger.Log($"Patching progress: {progress}");
                ProgressChanged?.Invoke(progress);
            };

            Logger.Log("Starting to patch");
            decoder.Run();

            Logger.Log("Patching done!");
            source.ChangeFormat(target);
        }
    }
}
