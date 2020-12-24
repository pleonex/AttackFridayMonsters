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
    using System.Linq;
    using System.Security.Cryptography;
    using SceneGate.Lemon.Containers.Converters;
    using SceneGate.Lemon.Titles;
    using Yarhl.IO;

    public static class GameVerifier
    {
        public static FilePatchStatus Verify(GameNode game)
        {
            TitleMetadata title;
            try {
                game.Root.TransformWith<BinaryCia2NodeContainer>();
                Logger.Log("Transformed to CIA!");

                title = game.Root.Children["title"]
                    .TransformWith<Binary2TitleMetadata>()
                    .GetFormatAs<TitleMetadata>();
                Logger.Log("Transformed to TitleMetadata");
            } catch (Exception ex) {
                Logger.Log("Invalid format.");
                Logger.Log(ex.ToString());
                return FilePatchStatus.InvalidFormat;
            }

            string titleId = title.TitleId.ToString("X16");
            Logger.Log($"Title ID: {titleId}");

            var invalidTitle = game.GamePatch.InvalidFiles.FirstOrDefault(f => f.TitleId == titleId);
            if (invalidTitle != null) {
                Logger.Log($"Known invalid title ID: {invalidTitle.Reason}");
                return invalidTitle.Reason;
            }

            var patches = game.GamePatch.Patches.Where(p => p.TitleId == titleId);
            if (!patches.Any()) {
                Logger.Log("Couldn't find any patch for this title ID");
                return FilePatchStatus.InvalidTitle;
            }

            var patch = patches.FirstOrDefault(p => p.TitleVersion == title.TitleVersion);
            if (patch == null) {
                Logger.Log($"No known versions: {title.TitleVersion}");
                return FilePatchStatus.InvalidVersion;
            }

            var programNode = game.Root.Children["content"].Children["program"];
            if (programNode.Tags.ContainsKey("LEMON_NCCH_ENCRYPTED")) {
                Logger.Log($"Encrypted game! {programNode.Tags["LEMON_NCCH_ENCRYPTED"]}");
                return FilePatchStatus.GameIsEncrypted;
            }

            string actualHash = GetHash(programNode.Stream);
            Logger.Log($"File hash: {actualHash}");

            var invalidFile = game.GamePatch.InvalidFiles.FirstOrDefault(f => f.Hash == actualHash);
            if (invalidFile != null) {
                Logger.Log($"Known invalid hash: {invalidFile.Reason}");
                return invalidFile.Reason;
            }

            if (actualHash != patch.SourceHash) {
                Logger.Log($"File doesn't match hash: {patch.SourceHash}");
                return FilePatchStatus.InvalidDump;
            }

            Logger.Log("Valid file!");
            return FilePatchStatus.ValidFile;
        }

        private static string GetHash(DataStream stream)
        {
            Logger.Log($"Hash for offset:{stream.Offset}, size: {stream.Length}");
            using var md5 = MD5.Create();

            // Just below LOH
            byte[] buffer = new byte[70 * 1024];

            int read;
            stream.Position = 0;
            while (stream.Length - stream.Position > buffer.Length) {
                read = BlockRead(stream, buffer);
                md5.TransformBlock(buffer, 0, read, buffer, 0);
            }

            read = BlockRead(stream, buffer);
            md5.TransformFinalBlock(buffer, 0, read);
            byte[] hash = md5.Hash;

            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        private static int BlockRead(DataStream stream, byte[] buffer)
        {
            int read;
            if (stream.Position + buffer.Length > stream.Length) {
                read = (int)(stream.Length - stream.Position);
            } else {
                read = buffer.Length;
            }

            stream.Read(buffer, 0, read);
            return read;
        }
    }
}