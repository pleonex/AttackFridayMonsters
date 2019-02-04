//  Program.cs
//
//  Author:
//       Benito Palacios Sanchez <benito356@gmail.com>
//
//  Copyright (c) 2017 Benito Palacios Sanchez
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
    using AttackFridayMonsters.Formats.Container;
    using AttackFridayMonsters.Formats.Text;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.IO;
    using Yarhl.Media.Text;

    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 4) {
                Console.WriteLine("USAGE: AttackFridayMonsters -i format input output");
                return;
            }

            string operation = args[0];
            string format = args[1];
            string input = args[2];
            string output = args[3];

            if (operation == "-i") {
                Import(format, input, output);
            } else {
                Console.WriteLine("Unknown operation");
                return;
            }
        }

        static void Import(string format, string input, string output)
        {
            switch (format.ToLower()) {
                case "darc":
                    var darcRoot = NodeFactory.CreateContainer("root");
                    foreach (string filePath in Directory.GetFiles(input, "*", SearchOption.AllDirectories)) {
                        string parent = Path.GetDirectoryName(filePath);
                        NodeFactory.CreateContainersForChild(darcRoot, parent.Replace(input, string.Empty), NodeFactory.FromFile(filePath));
                    }

                    var darcFormat = new NodeContainerFormat();
                    darcFormat.Root.Add(darcRoot.Children);
                    darcFormat.ConvertWith<DarcToBinary, NodeContainerFormat, BinaryFormat>()
                              .Stream.WriteTo(output);
                    break;

                case "ofs3":
                    var ofs3Root = NodeFactory.FromDirectory(input);
                    var ofs3Format = new NodeContainerFormat();
                    ofs3Format.Root.Add(ofs3Root.Children);
                    ofs3Format.ConvertWith<Ofs3ToBinary, NodeContainerFormat, BinaryFormat>()
                              .Stream.WriteTo(output);
                    break;

                case "script":
                    var container = DecompressLzx(output, "-d")
                        .ConvertWith<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>();
                    var oldBinScript = container.Root.Children["File1.bin"];
                    var converter = new ScriptToPo {
                        OriginalScript = oldBinScript.GetFormatAs<BinaryFormat>().Stream,
                    };

                    var newBinScript = NodeFactory.FromFile(input)
                        .Transform<Po2Binary, BinaryFormat, Po>()
                        .Transform<BinaryFormat, Po>(converter);

                    oldBinScript.GetFormatAs<BinaryFormat>().Stream.Dispose();
                    oldBinScript.Format.Dispose();
                    oldBinScript.Format = newBinScript.Format;

                    container.ConvertWith<Ofs3ToBinary, NodeContainerFormat, BinaryFormat>()
                             .Stream.WriteTo(output);
                    DecompressLzx(output, "-evb").Stream.WriteTo(output);
                    break;

                case "episode":
                    string tempEpisode = Path.GetTempFileName();
                    File.Copy(output, tempEpisode, true);
                    var episodeContainer = new BinaryFormat(tempEpisode)
                        .ConvertWith<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>();

                    var oldEpisode = episodeContainer.Root.Children["epsetting.dat"];
                    var episodeConverter = new EpisodeSettingsToPo {
                        Original = oldEpisode.GetFormatAs<BinaryFormat>().Stream,
                    };

                    var newEpisode = NodeFactory.FromFile(input)
                        .Transform<Po2Binary, BinaryFormat, Po>()
                        .Transform<Po, BinaryFormat>(converter: episodeConverter);

                    oldEpisode.Format = newEpisode.Format;
                    episodeContainer.ConvertWith<NodeContainerFormat, BinaryFormat>(new Ofs3ToBinary { Padding = 0x10 })
                             .Stream.WriteTo(output);
                    break;

                // case "carddata0":
                //     var carddata0 = new BinaryFormat();
                //     using (var outputStream = new DataStream(output, FileOpenMode.Read))
                //         outputStream.WriteTo(carddata0.Stream);

                //     var carddata0Container = carddata0
                //         .ConvertWith<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>();
                //     carddata0Container.Root.Children["File0.bin"].Format =
                //                           new BinaryFormat(input)
                //                           .ConvertWith<Po2Binary, BinaryFormat, Po>()
                //                           .ConvertWith<Po, BinaryFormat>(new CardDataToPo(0));
                //     carddata0Container.ConvertWith<NodeContainerFormat, BinaryFormat>(new Ofs3ToBinary() { Padding = 0x10 })
                //         .Stream.WriteTo(output);
                //     break;

                // case "carddata25":
                //     var carddata25 = new BinaryFormat();
                //     using (var outputStream = new DataStream(output, FileOpenMode.Read))
                //         outputStream.WriteTo(carddata25.Stream);

                //     var carddata25Container = carddata25
                //         .ConvertWith<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>();
                //     carddata25Container.Root.Children["File25.bin"].Format =
                //                           new BinaryFormat(input)
                //                           .ConvertWith<Po2Binary, BinaryFormat, Po>()
                //                           .ConvertWith<Po, BinaryFormat>(new CardDataToPo(25));
                //     carddata25Container.ConvertWith<NodeContainerFormat, BinaryFormat>(new Ofs3ToBinary { Padding = 0x10 })
                //         .Stream.WriteTo(output);
                //     break;

                case "bclyt":
                    using (var original = new BinaryFormat()) {
                        // Dump input to memory
                        using (var outputStream = new DataStream(output, FileOpenMode.Read))
                            outputStream.WriteTo(original.Stream);

                        // input -> binary -> po + original -> bclyt -> output
                        var bclytPo = NodeFactory.FromFile(input)
                            .Transform<Po2Binary, BinaryFormat, Po>()
                            .GetFormatAs<Po>();
                        Format.ConvertWith<BclytToPo, Tuple<BinaryFormat, Po>, BinaryFormat>(
                            new Tuple<BinaryFormat, Po>(original, bclytPo))
                              .Stream.WriteTo(output);
                    }

                    break;
            }
        }

        static BinaryFormat DecompressLzx(string file, string mode)
        {
            string tempFile = Path.GetTempFileName();
            File.Copy(file, tempFile, true);

            string program = "lzx.exe";
            string arguments = mode + " " + tempFile;
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) {
                program = "lzx";
            }

            Process process = new Process();
            process.StartInfo.FileName = program;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            process.WaitForExit();

            DataStream fileStream = new DataStream(tempFile, FileOpenMode.Read);
            DataStream memoryStream = new DataStream();
            fileStream.WriteTo(memoryStream);

            fileStream.Dispose();
            File.Delete(tempFile);

            return new BinaryFormat(memoryStream);
        }
    }
}
