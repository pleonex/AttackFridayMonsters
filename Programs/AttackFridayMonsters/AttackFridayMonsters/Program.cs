//
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
    using Formats.Container;
    using Formats.Text;
    using Yarhl.IO;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.Media.Text;

    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 4) {
                Console.WriteLine("USAGE: AttackFridayMonsters -e format input output");
                return;
            }

            string operation = args[0];
            string format = args[1];
            string input = args[2];
            string output = args[3];

            if (operation == "-e") {
                Export(format, input, output);
            } else if (operation =="-i") {
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
                        NodeFactory.CreateContainersForChild(darcRoot, parent.Replace(input, ""), NodeFactory.FromFile(filePath));
                    }
                    var darcFormat = new NodeContainerFormat();
                    darcFormat.Root.Add(darcRoot.Children);
                    darcFormat.ConvertWith<BinaryFormat>(new DarcToBinary())
                              .Stream.WriteTo(output);
                    break;

                case "ofs3":
                    var ofs3Root = NodeFactory.FromDirectory(input);
                    var ofs3Format = new NodeContainerFormat();
                    ofs3Format.Root.Add(ofs3Root.Children);
                    ofs3Format.ConvertWith<BinaryFormat>(new Ofs3ToBinary())
                              .Stream.WriteTo(output);
                    break;

                case "script":
                    var container = DecompressLzx(output, "-d")
                        .ConvertWith<NodeContainerFormat>(new Ofs3ToBinary());
                    var oldBinScript = container.Root.Children["File1.bin"];
                    var converter = new ScriptToPo {
                        OriginalScript = oldBinScript.GetFormatAs<BinaryFormat>().Stream };

                    var newBinScript = NodeFactory.FromFile(input)
                        .Transform<BinaryFormat, Po, Po2Binary>()
                        .Transform<BinaryFormat>(converter: converter);

                    oldBinScript.GetFormatAs<BinaryFormat>().Stream.Dispose();
                    oldBinScript.Format.Dispose();
                    oldBinScript.Format = newBinScript.Format;

                    container.ConvertWith<BinaryFormat>(new Ofs3ToBinary())
                             .Stream.WriteTo(output);
                    DecompressLzx(output, "-evb").Stream.WriteTo(output);
                    break;

                case "episode":
                    string tempEpisode = Path.GetTempFileName();
                    File.Copy(output, tempEpisode, true);
                    var episodeContainer = new BinaryFormat(tempEpisode)
                        .ConvertWith<NodeContainerFormat>(new Ofs3ToBinary());

                    var oldEpisode = episodeContainer.Root.Children["epsetting.dat"];
                    var episodeConverter = new EpisodeSettingsToPo {
                        Original = oldEpisode.GetFormatAs<BinaryFormat>().Stream
                    };

                    var newEpisode = NodeFactory.FromFile(input)
                        .Transform<BinaryFormat, Po, Po2Binary>()
                        .Transform<BinaryFormat>(converter: episodeConverter);

                    oldEpisode.Format = newEpisode.Format;
                    episodeContainer.ConvertWith<BinaryFormat>(new Ofs3ToBinary { Padding = 0x10 })
                             .Stream.WriteTo(output);
                    break;

                case "carddata0":
                    var carddata0 = new BinaryFormat();
                    using (var outputStream = new DataStream(output, FileOpenMode.Read))
                        outputStream.WriteTo(carddata0.Stream);

                    var carddata0Container = carddata0
                        .ConvertWith<NodeContainerFormat>(new Ofs3ToBinary());
                    carddata0Container.Root.Children["File0.bin"].Format =
                                          new BinaryFormat(input)
                                          .ConvertWith<Po>(new Po2Binary())
                                          .ConvertWith<BinaryFormat>(new CardDataToPo(0));
                    carddata0Container.ConvertWith<BinaryFormat>(new Ofs3ToBinary() { Padding = 0x10 })
                        .Stream.WriteTo(output);
                    break;

                case "carddata25":
                    var carddata25 = new BinaryFormat();
                    using (var outputStream = new DataStream(output, FileOpenMode.Read))
                        outputStream.WriteTo(carddata25.Stream);

                    var carddata25Container = carddata25
                        .ConvertWith<NodeContainerFormat>(new Ofs3ToBinary());
                    carddata25Container.Root.Children["File25.bin"].Format =
                                          new BinaryFormat(input)
                                          .ConvertWith<Po>(new Po2Binary())
                                          .ConvertWith<BinaryFormat>(new CardDataToPo(25));
                    carddata25Container.ConvertWith<BinaryFormat>(new Ofs3ToBinary { Padding = 0x10 })
                        .Stream.WriteTo(output);
                    break;

                case "bclyt":
                    using (var original = new BinaryFormat()) {
                        // Dump input to memory
                        using (var outputStream = new DataStream(output, FileOpenMode.Read))
                            outputStream.WriteTo(original.Stream);

                        // input -> binary -> po + original -> bclyt -> output
                        var bclytPo = NodeFactory.FromFile(input)
                            .Transform<BinaryFormat, Po, Po2Binary>()
                            .GetFormatAs<Po>();
                        Format.ConvertWith<BinaryFormat>(
                            new Tuple<BinaryFormat, Po>(original, bclytPo),
                            new BclytToPo())
                              .Stream.WriteTo(output);
                    }
                    break;
            }
        }

        static void Export(string format, string input, string output)
        {
            BinaryFormat inputFormat = new BinaryFormat(input);
            switch (format.ToLower()) {
                case "lzx_ofs3":
                    inputFormat.Stream.Dispose();
                    inputFormat = DecompressLzx(input, "-d");
                    goto case "ofs3";

                case "ofs3":
                    var folder = inputFormat
                        .ConvertWith<NodeContainerFormat>(new Ofs3ToBinary())
                        .Root;

                    Directory.CreateDirectory(output);
                    foreach (var child in folder.Children) {
                        string outputFile = Path.Combine(output, child.Name);
                        child.GetFormatAs<BinaryFormat>().Stream.WriteTo(outputFile);
                    }
                    break;

                case "episode":
                    inputFormat
                        .ConvertWith<NodeContainerFormat>(new Ofs3ToBinary())
                        .Root.Children["epsetting.dat"].Format
                        .ConvertWith<Po>(new EpisodeSettingsToPo())
                        .ConvertWith<BinaryFormat>(new Po2Binary()).Stream.WriteTo(output);
                    break;

                case "carddata0":
                    var carddata0 = inputFormat
                        .ConvertWith<NodeContainerFormat>(new Ofs3ToBinary())
                        .Root;

                    carddata0.Children["File0.bin"].Format
                             .ConvertWith<Po>(new CardDataToPo(0))
                             .ConvertWith<BinaryFormat>(new Po2Binary()).Stream.WriteTo(output);
                    break;

                case "carddata25":
                    var carddata25 = inputFormat
                        .ConvertWith<NodeContainerFormat>(new Ofs3ToBinary())
                        .Root;

                    carddata25.Children["File25.bin"].Format
                              .ConvertWith<Po>(new CardDataToPo(25))
                              .ConvertWith<BinaryFormat>(new Po2Binary()).Stream.WriteTo(output);
                    break;

                case "script":
                    inputFormat.Stream.Dispose();
                    inputFormat = DecompressLzx(input, "-d");

                    var binScript = inputFormat
                        .ConvertWith<NodeContainerFormat>(new Ofs3ToBinary())
                        .Root.Children["File1.bin"].GetFormatAs<BinaryFormat>();

                    // Ignore empty scripts
                    if (binScript.Stream.Length > 0)
                        binScript.ConvertWith<Po>(new ScriptToPo())
                                 .ConvertWith<BinaryFormat>(new Po2Binary())
                                 .Stream.WriteTo(output);
                    else
                        Console.WriteLine("No script for " + input);
                    break;

                case "darc":
                    var darcRoot = inputFormat
                        .ConvertWith<NodeContainerFormat>(new DarcToBinary()).Root;

                    string basePath = darcRoot.Children[0].Path; // root darc
                    foreach (var child in Navigator.IterateNodes(darcRoot)) {
                        if (!(child.Format is BinaryFormat))
                            continue;

                        string path = child.Parent.Path.Replace(basePath, "").TrimStart('/');
                        string outputDir = Path.Combine(output, path);
                        string outputFile = Path.Combine(outputDir, child.Name);
                        if (!Directory.Exists(outputDir))
                            Directory.CreateDirectory(outputDir);
                        child.GetFormatAs<BinaryFormat>()?.Stream.WriteTo(outputFile);
                    }
                    break;

                case "bclyt":
                    inputFormat.ConvertWith<Po>(new BclytToPo())
                               .ConvertWith<BinaryFormat>(new Po2Binary())
                               .Stream.WriteTo(output);
                    break;

                default:
                    Console.WriteLine("Unsupported format");
                    break;
            }

            inputFormat.Stream.Dispose();
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
