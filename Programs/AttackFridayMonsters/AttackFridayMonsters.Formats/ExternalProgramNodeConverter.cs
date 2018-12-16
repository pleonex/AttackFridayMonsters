//  ExternalProgramNodeConverter.cs
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
    using Yarhl.FileSystem;
    using Yarhl.IO;

    public class ExternalProgramNodeConverter
        : IConverter<BinaryFormat, NodeContainerFormat>
    {
        public string Program { get; set; }

        public string Arguments { get; set; }

        public string OutputDirectory { get; set; }

        public string WorkingDirectory { get; set; }

        public bool WorkingDirectoryAsOutput { get; set; }

        public NodeContainerFormat Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (!Arguments.Contains("<in>"))
                throw new FormatException("Missing input in arguments");
            if (!Arguments.Contains("<out>") && !WorkingDirectoryAsOutput)
                throw new FormatException("Missing output in arguments");

            // Save stream into temporal file
            string tempInputFile = Path.GetTempFileName();
            source.Stream.WriteTo(tempInputFile);

            // Get or create the output file
            string tempOutputPath;
            if (!string.IsNullOrEmpty(OutputDirectory)) {
                tempOutputPath = OutputDirectory;
            } else if (WorkingDirectoryAsOutput) {
                if (string.IsNullOrEmpty(WorkingDirectory))
                    throw new ArgumentNullException(nameof(WorkingDirectory));

                tempOutputPath = WorkingDirectory;
            } else {
                tempOutputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            }

            Directory.CreateDirectory(tempOutputPath);

            // Run the process
            string args = Arguments.Replace("<in>", tempInputFile)
                .Replace("<out>", tempOutputPath);

            var process = new Process();
            process.StartInfo.FileName = Program;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.RedirectStandardOutput = false;

            if (!string.IsNullOrEmpty(WorkingDirectory)) {
                if (!Directory.Exists(WorkingDirectory)) {
                    Directory.CreateDirectory(WorkingDirectory);
                }

                process.StartInfo.WorkingDirectory = WorkingDirectory;
            }

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0) {
                DeleteIfExists(tempInputFile);
                DeleteIfExists(tempOutputPath);
                throw new Exception($"Error running: {Program} {args}");
            }

            DeleteIfExists(tempInputFile);
            return NodeFactory.FromDirectory(tempOutputPath)
                .GetFormatAs<NodeContainerFormat>();
        }

        static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }
}