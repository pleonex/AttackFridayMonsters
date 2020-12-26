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
namespace Patcher.Resources
{
    using System;
    using Yarhl.FileFormat;
    using Yarhl.IO;
    using Yarhl.Media.Text;

    public static class L10n
    {
        private const string Language = "es";
        private readonly static Po po = LoadPo(Language);

        public static string Get(string original, string context = null)
        {
            if (po == null) {
                return original;
            }

            PoEntry entry = po.FindEntry(original, context);
            if (entry == null) {
                Logger.Log($"PO is missing entry for: {original} || {context}");
                return original;
            }

            if (string.IsNullOrEmpty(entry.Translated)) {
                Logger.Log($"PO is missing translation for: {original} || {context}");
                return original;
            }

            return entry.Translated;
        }

        private static Po LoadPo(string language)
        {
            string resourceName = $"{ResourcesName.Prefix}.{language}.po";

            var assembly = typeof(L10n).Assembly;
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) {
                Logger.Log($"Cannot find language resource: {resourceName}");
                return null;
            }

            try {
                using var binaryPo = new BinaryFormat(DataStreamFactory.FromStream(stream));
                return (Po)ConvertFormat.With<Binary2Po>(binaryPo);
            } catch (Exception ex) {
                Logger.Log($"Error parsing language resource: {ex}");
                return null;
            }
        }
    }
}
