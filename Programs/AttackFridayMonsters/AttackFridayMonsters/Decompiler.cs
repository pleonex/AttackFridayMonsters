//  Decompiler.cs
//
//  Author:
//       Benito Palacios Sanchez <benito356@gmail.com>
//
//  Copyright (c) 2018 Benito Palacios Sanchez
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
namespace AttackFridayMonsters
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using AttackFridayMonsters.Formats.Container;
    using AttackFridayMonsters.Formats.Text;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.Media.Text;

    static class Decompiler
    {
        public static void Export(string gameDir, string outputDir, string toolsDir)
        {
            string fontDir = Path.Combine(outputDir, "fonts");
            string textDir = Path.Combine(outputDir, "texts");
            string scriptDir = Path.Combine(textDir, "scripts");

            Node root = NodeFactory.FromDirectory(gameDir, "*", "root", true);
            if (root.Children.Count == 0) {
                Console.WriteLine("Game folder is empty!");
                return;
            }

            Console.WriteLine("1. Unpacking files");
            Node title = Navigator.SearchFile(root, "/root/data/gkk/lyt/title.arc")
                .Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();
            Node carddata = Navigator.SearchFile(root, "/root/data/gkk/cardgame/carddata.bin")
                .Transform<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>();
            Node episode = Navigator.SearchFile(root, "/root/data/gkk/episode/episode.bin")
                .Transform<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>();

            Console.WriteLine("2. Exporting fonts");
            string fontPath = Path.Combine(fontDir, "kk_KN_Font.bcfnt");
            string fontTool = Path.Combine(toolsDir, "bcfnt.py");
            title.Children["font"]
                .Children["kk_KN_Font.bcfnt"]
                .Stream.WriteTo(fontPath);
            RunProgram("python", $"{fontTool} -x -y -f {fontPath}", fontDir);
            File.Delete(fontPath);

            Console.WriteLine("3. Export texts");
            Console.WriteLine("3.1. Export card texts");
            carddata.Children["File0.bin"]
                .Transform<BinaryFormat, Po>(CardDataToPo.CreateForId(0))
                .Transform<Po2Binary, Po, BinaryFormat>()
                .Stream.WriteTo(Path.Combine(textDir, "cardinfo.po"));
            carddata.Children["File25.bin"]
                .Transform<BinaryFormat, Po>(CardDataToPo.CreateForId(25))
                .Transform<Po2Binary, Po, BinaryFormat>()
                .Stream.WriteTo(Path.Combine(textDir, "cardgame_dialogs.po"));

            Console.WriteLine("3.2. Export story chapters");
            episode.Children["epsetting.dat"]
                .Transform<EpisodeSettingsToPo, BinaryFormat, Po>()
                .Transform<Po2Binary, Po, BinaryFormat>()
                .Stream.WriteTo(Path.Combine(textDir, "episodes_title.po"));

            Console.WriteLine("3.3. Extract scripts");
            string lzxTool = Path.Combine(toolsDir, "lzx");
            var maps = Navigator.SearchFile(root, "/root/data/gkk/map_gz")
                    .Children
                    .Where(n => n.Name[0] == 'A' || n.Name[0] == 'B');
            foreach (var compressedMap in maps) {
                string mapName = Path.GetFileNameWithoutExtension(compressedMap.Name);

                // Decompress the file
                string mapFile = Path.Combine(scriptDir, compressedMap.Name);
                compressedMap.Stream.WriteTo(mapFile);
                RunProgram(lzxTool, $"-d {mapFile}");
                using (Node map = NodeFactory.FromFile(mapFile)) {
                    Node script = map
                        .Transform<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>()
                        .Children["File1.bin"];
                    if (script.Stream.Length > 0) {
                        script.Transform<ScriptToPo, BinaryFormat, Po>()
                            .Transform<Po2Binary, Po, BinaryFormat>()
                            .Stream.WriteTo(Path.Combine(scriptDir, mapName + ".po"));
                    }
                }

                File.Delete(mapFile);
            }

            Console.WriteLine("3.4. Extract text from code");
            Console.WriteLine("[WARNING] TODO");

            Console.WriteLine("3.5. Extract text from bclyt");
            Console.WriteLine("[WARNING] TODO");
        }

        static void RunProgram(string command, string args, string cwd = null)
        {
            var process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.RedirectStandardOutput = false;

            if (!string.IsNullOrEmpty(cwd)) {
                process.StartInfo.WorkingDirectory = cwd;
            }

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception($"Error running program: {command} {args}");
        }
    }
}
