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
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Patcher.Resources;
    using Xdelta;
    using Yarhl.FileSystem;

    public class GamePatcher
    {
        private static GamePatch info = LoadPatchInfo();

        public event ProgressChangedHandler ProgressChanged;

        public event FinishedHandler FinishedHandler;

        public async Task PatchAsync(string intputFile, string outputFile)
        {
            if (string.IsNullOrWhiteSpace(intputFile))
                throw new ArgumentNullException(nameof(intputFile));
            if (string.IsNullOrWhiteSpace(outputFile))
                throw new ArgumentNullException(nameof(outputFile));

            PatchInfo patchInfo = GetPatchInfo(intputFile);
            await PatchAsync(intputFile, patchInfo, outputFile).ConfigureAwait(false);
        }

        private static GamePatch LoadPatchInfo()
        {
            string text;
            var assembly = typeof(GamePatcher).Assembly;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(ResourcesName.Patches))) {
                text = reader.ReadToEnd();
            }

            return System.Text.Json.JsonSerializer.Deserialize<GamePatch>(text);
        }

        private async Task PatchAsync(string inputFile, PatchInfo patchInfo, string outputFile)
        {
            var assembly = typeof(GamePatcher).Assembly;
            using Stream patch = assembly.GetManifestResourceStream(patchInfo.ResourcePath);

            using FileStream source = FileStreamFactory.OpenForRead(inputFile);
            using FileStream target = FileStreamFactory.CreateForWriteAndRead(outputFile);

            Decoder decoder = new Decoder(source, patch, target);
            decoder.ProgressChanged += progress => ProgressChanged?.Invoke(progress);
            decoder.Finished += () => FinishedHandler?.Invoke();

            await Task.Run(() => decoder.Run()).ConfigureAwait(false);
        }

        private static PatchInfo GetPatchInfo(string file)
        {
            string actualHash = GetHash(file);
            PatchInfo patchInfo = info.Patches.FirstOrDefault(p => p.SourceHash == actualHash);

            if (patchInfo is null) {
                string reason = GetInvalidFileReason(file, actualHash);
                throw new FormatException(reason);
            }

            return patchInfo;
        }

        private static string GetHash(string file)
        {
            using var md5 = MD5.Create();
            using var stream = FileStreamFactory.OpenForRead(file);
            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        private static string GetInvalidFileReason(string file, string hash)
        {
            InvalidFileInfo reason = info.InvalidFiles.FirstOrDefault(f => f.Hash == hash);
            if (reason != null) {
                return reason.Reason.ToString();
            }

            return "Unknown";
        }
    }
}
