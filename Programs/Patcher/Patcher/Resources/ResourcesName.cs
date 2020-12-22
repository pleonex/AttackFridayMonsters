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
    public static class ResourcesName
    {
        public static string Prefix = typeof(ResourcesName).Namespace;

        public static string Icon => Prefix + ".icon.png";

        public static string MainBackground => Prefix + ".bg.png";

        public static string CreditsBackground => Prefix + ".bg_credits_with_text.png";

        public static string Patches => Prefix + ".patches.json";

        public static string Clippy => Prefix + "." + L10n.Get("clippy_en.png");
    }
}