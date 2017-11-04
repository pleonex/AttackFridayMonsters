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
    using System.IO;
    using Formats.Container;
    using Formats.Text;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.Media.Text;

    class MainClass
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
                Convert(format, input, output);
            } else {
                Console.WriteLine("Unknown operation");
                return;
            }
        }

        static void Convert(string format, string input, string output)
        {
            BinaryFormat inputFormat = new BinaryFormat(input);
            switch (format.ToLower()) {
                case "ofs3":
                    var folder = inputFormat
                        .ConvertWith<NodeContainerFormat>(new Ofs3ToBinaryConverter())
                        .Root;

                    Directory.CreateDirectory(output);
                    foreach (var child in folder.Children) {
                        string outputFile = Path.Combine(output, child.Name);
                        child.GetFormatAs<BinaryFormat>().Stream.WriteTo(outputFile);
                    }
                    break;

                case "episode":
                    inputFormat
                        .ConvertWith<NodeContainerFormat>(new Ofs3ToBinaryConverter())
                        .Root.Children["epsetting.dat"].Format
                        .ConvertWith<Po>(new EpisodeSettingsToPo())
                        .ConvertTo<BinaryFormat>().Stream.WriteTo(output);
                    break;

                default:
                    Console.WriteLine("Unsupported format");
                    break;
            }
        }
    }
}
