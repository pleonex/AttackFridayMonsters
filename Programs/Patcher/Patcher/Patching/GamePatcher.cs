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
    using Patcher.Resources;
    using Xdelta;
    using Yarhl.FileSystem;

    public class GamePatcher
    {
        public static GamePatch Patch { get; } = LoadPatchInfo();

        public event ProgressChangedHandler ProgressChanged;

        public event FinishedHandler FinishedHandler;

        // public async Task PatchAsync(GameNode game)
        // {
        //     var assembly = typeof(GamePatcher).Assembly;
        //     using Stream patch = assembly.GetManifestResourceStream(game.PatchInfo.ResourcePath);

        //     // using FileStream source = FileStreamFactory.OpenForRead(inputFile);
        //     // var target = new MemoryStream();

        //     // Decoder decoder = new Decoder(source, patch, target);
        //     // decoder.ProgressChanged += progress => ProgressChanged?.Invoke(progress);
        //     // decoder.Finished += () => FinishedHandler?.Invoke();

        //     // await Task.Run(() => decoder.Run()).ConfigureAwait(false);
        // }

        private static GamePatch LoadPatchInfo()
        {
            string text;
            var assembly = typeof(GamePatcher).Assembly;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(ResourcesName.Patches))) {
                text = reader.ReadToEnd();
            }

            return System.Text.Json.JsonSerializer.Deserialize<GamePatch>(text);
        }
    }
}
