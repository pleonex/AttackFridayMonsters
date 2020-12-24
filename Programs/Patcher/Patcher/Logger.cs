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
namespace Patcher
{
    using System;
    using System.IO;

    public static class Logger
    {
        private static readonly string LogFile = GetLogFile();

        public static void Log(string message)
        {
            try {
                Console.WriteLine(message);
                File.AppendAllText(LogFile, message + "\n"); // there are really many better ways to do this
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        private static string GetLogFile()
        {
            string appPath = Path.GetDirectoryName(typeof(Logger).Assembly.Location);
            return Path.Combine(appPath, "logs.txt");
        }
    }
}