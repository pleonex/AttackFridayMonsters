//  ExternalProgramConverter.cs
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
namespace AttackFridayMonsters.Formats
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Yarhl.FileFormat;
    using Yarhl.IO;

    public class ExternalProgramConverter : IConverter<BinaryFormat, BinaryFormat>
    {
        public string Program { get; set; }

        public string Arguments { get; set; }

        public string WorkingDirectory { get; set; }

        public string FileName { get; set; }

        public BinaryFormat Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (!Arguments.Contains("<in>") && !Arguments.Contains("<inout>"))
                throw new FormatException("Missing input in arguments");

            // Save stream into temporal file
            string tempInputFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempInputFolder);

            string tempInputName = string.IsNullOrEmpty(FileName) ? "input.bin" : FileName;
            string tempInputFile = Path.Combine(tempInputFolder, tempInputName);
            source.Stream.WriteTo(tempInputFile);

            // Get or create the output file
            string tempOutputFile;
            if (Arguments.Contains("<out>")) {
                tempOutputFile = Path.GetTempFileName();
            } else if (Arguments.Contains("<inout>")) {
                tempOutputFile = tempInputFile;
            } else {
                throw new FormatException("Missing output in arguments");
            }

            // Run the process
            string args = Arguments.Replace("<in>", tempInputFile)
                .Replace("<inout>", tempInputFile)
                .Replace("<out>", tempOutputFile);

            var process = new Process();
            process.StartInfo.FileName = Program;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            if (!string.IsNullOrEmpty(WorkingDirectory)) {
                if (!Directory.Exists(WorkingDirectory)) {
                    Directory.CreateDirectory(WorkingDirectory);
                }

                process.StartInfo.WorkingDirectory = WorkingDirectory;
            }

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0) {
                Directory.Delete(tempInputFolder, true);
                DeleteIfExists(tempOutputFile);
                throw new Exception($"Error running: {Program} {args} {process.StandardError.ReadToEnd()} {process.StandardOutput.ReadToEnd()}");
            }

            // Read the file into memory so we can delete it.
            var convertedStream = new BinaryFormat();
            using (var tempStream = DataStreamFactory.FromFile(tempOutputFile, FileOpenMode.Read)) {
                tempStream.WriteTo(convertedStream.Stream);
            }

            Directory.Delete(tempInputFolder, true);
            DeleteIfExists(tempOutputFile);
            return convertedStream;
        }

        static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }
}