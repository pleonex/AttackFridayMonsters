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
    using System.Reflection;
    using AttackFridayMonsters.Formats;
    using AttackFridayMonsters.Formats.Container;
    using AttackFridayMonsters.Formats.Text;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.Media.Text;

    public class Decompiler
    {
        public Decompiler()
        {
            string appDir = Assembly.GetExecutingAssembly().CodeBase;
            GameDirectory = Path.Combine(appDir, "gamedata");
            OutputDirectory = Path.Combine(appDir, "extracted");
            ToolsDirectory = Path.Combine(appDir, "tools");
        }

        public string GameDirectory { get; set; }

        public string OutputDirectory { get; set; }

        public string ToolsDirectory { get; set; }

        public string ImageDirectory {
            get { return Path.Combine(OutputDirectory, "images"); }
        }

        public string FontDirectory {
            get { return Path.Combine(OutputDirectory, "fonts"); }
        }

        public string TextDirectory {
            get { return Path.Combine(OutputDirectory, "texts"); }
        }

        public string ScriptDirectory {
            get { return Path.Combine(TextDirectory, "scripts"); }
        }

        public void Export()
        {
            Node root = NodeFactory.FromDirectory(GameDirectory, "*", "root", true);
            if (root.Children.Count == 0) {
                Console.WriteLine("Game folder is empty!");
                return;
            }

            Console.WriteLine("1. Unpacking files");
            Node title = Navigator.SearchFile(root, "/root/data/gkk/lyt/title.arc")
                .Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();
            Node bumper = Navigator.SearchFile(root, "/root/data/gkk/lyt/bumper.arc")
                .Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();
            Node movieLow = Navigator.SearchFile(root, "/root/data/gkk/lyt/movie_low.arc")
                .Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();
            Node notebook = Navigator.SearchFile(root, "/root/data/gkk/lyt/notebook.arc")
                .Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();
            Node selwin = Navigator.SearchFile(root, "/root/data/gkk/lyt/selwin.arc")
                .Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();
            Node subscreen = Navigator.SearchFile(root, "/root/data/gkk/lyt/sub_screen.bin")
                .Transform<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>();
            Node subsceen0 = subscreen.Children["File0.bin"]
                .Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();

            Node carddata = Navigator.SearchFile(root, "/root/data/gkk/cardgame/carddata.bin")
                .Transform<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>();
            Node cardlyt = Navigator.SearchFile(root, "/root/data/gkk/cardgame/cardlyt_d.arc")
                .Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();
            Node cardtext = Navigator.SearchFile(root, "/root/data/gkk/cardgame/cardtex.bin")
                .Transform<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>();

            Node episode = Navigator.SearchFile(root, "/root/data/gkk/episode/episode.bin")
                .Transform<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>();

            Console.WriteLine("2. Exporting fonts");
            string fontTool = Path.Combine(ToolsDirectory, "bcfnt.py");
            var fontConverter = new ExternalProgramNodeConverter {
                Program = "python",
                Arguments = $"{fontTool} -x -y -f <in>",
                WorkingDirectory = FontDirectory,
                WorkingDirectoryAsOutput = true
            };

            string fontPath = Path.Combine(FontDirectory, "kk_KN_Font.bcfnt");
            title.Children["font"]
                .Children["kk_KN_Font.bcfnt"]
                .Transform<BinaryFormat, NodeContainerFormat>(fontConverter);

            Console.WriteLine("3. Export texts");
            Console.WriteLine("3.1. Export card texts");
            carddata.Children["File0.bin"]
                .Transform<BinaryFormat, Po>(CardDataToPo.CreateForId(0))
                .Transform<Po2Binary, Po, BinaryFormat>()
                .Stream.WriteTo(Path.Combine(TextDirectory, "cardinfo.po"));
            carddata.Children["File25.bin"]
                .Transform<BinaryFormat, Po>(CardDataToPo.CreateForId(25))
                .Transform<Po2Binary, Po, BinaryFormat>()
                .Stream.WriteTo(Path.Combine(TextDirectory, "cardgame_dialogs.po"));

            Console.WriteLine("3.2. Export story chapters");
            episode.Children["epsetting.dat"]
                .Transform<EpisodeSettingsToPo, BinaryFormat, Po>()
                .Transform<Po2Binary, Po, BinaryFormat>()
                .Stream.WriteTo(Path.Combine(TextDirectory, "episodes_title.po"));

            Console.WriteLine("3.3. Extract scripts");
            var compressionConverter = new ExternalProgramConverter {
                Program = Path.Combine(ToolsDirectory, "lzx"),
                Arguments = "-d <inout>",
            };

            var maps = Navigator.SearchFile(root, "/root/data/gkk/map_gz")
                    .Children
                    .Where(n => n.Name[0] == 'A' || n.Name[0] == 'B');
            foreach (var map in maps) {
                string mapName = Path.GetFileNameWithoutExtension(map.Name);
                string scriptFile = Path.Combine(ScriptDirectory, mapName + ".po");

                Node script = map
                    .Transform<BinaryFormat, BinaryFormat>(compressionConverter)
                    .Transform<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>()
                    .Children["File1.bin"];
                if (script.Stream.Length > 0) {
                    script.Transform<ScriptToPo, BinaryFormat, Po>()
                        .Transform<Po2Binary, Po, BinaryFormat>()
                        .Stream.WriteTo(scriptFile);
                }
            }

            Console.WriteLine("3.4. Extract text from code");
            Console.WriteLine("[WARNING] TODO");

            Console.WriteLine("3.5. Extract text from bclyt");
            Console.WriteLine("[WARNING] TODO");

            Console.WriteLine("4. Extract images");
            var titleImages = new[] {
                "timg/auto_mk.bclim",
                "timg/butji_mdr.bclim",
                "timg/butji_n.bclim",
                "timg/butji_y.bclim",
                "timg/epi_moji.bclim",
                "timg/head_load.bclim",
                "timg/head_save.bclim",
                "timg/title_rogo.bclim"
            };
            ExtractBclimImages(title, titleImages);

            var bumperImages = new[] {
                "timg/ex_msg_A.bclim",
                "timg/ex_msg_B.bclim",
                "timg/ex_msg_C.bclim",
                "timg/stereo_ji.bclim",
            };
            ExtractBclimImages(bumper, bumperImages);

            var cardlytImages = new[] {
                "timg/butji_mdr.bclim",
                "timg/butji_n.bclim",
                "timg/butji_y.bclim",
                "timg/chan_moji.bclim",
                "timg/dasu_butt.bclim",
                "timg/mai_1.bclim",
                "timg/mai_2.bclim",
                "timg/mai_3.bclim",
                "timg/mai_4.bclim",
                "timg/mai_5.bclim",
            };
            ExtractBclimImages(cardlyt, cardlytImages);

            var moviewLowImages = new[] {
                "timg/skip_ji.bclim",
            };
            ExtractBclimImages(movieLow, moviewLowImages);

            var notebookImages = new[] {
                "timg/auto_moji.bclim",
                "timg/butji_mdr.bclim",
                "timg/page_00.bclim",
                "timg/page_01.bclim",
                "timg/page_02a.bclim",
                "timg/page_02b.bclim",
                "timg/page_03.bclim",
                "timg/page_04.bclim",
                "timg/page_05a.bclim",
                "timg/page_05b.bclim",
                "timg/page_06a.bclim",
                "timg/page_06b.bclim",
                "timg/page_07.bclim",
                "timg/page_08.bclim",
                "timg/page_09.bclim",
                "timg/page_10.bclim",
                "timg/page_11.bclim",
                "timg/page_12.bclim",
                "timg/page_13.bclim",
                "timg/page_14.bclim",
            };
            ExtractBclimImages(notebook, notebookImages);

            var selwinImages = new[] {
                "timg/butji_n.bclim",
                "timg/butji_y.bclim",
            };
            ExtractBclimImages(selwin, selwinImages);

            var subscreen0Images = new[] {
                "timg/A_moji.bclim",
                "timg/auto_moji.bclim",
                "timg/butji_mdr.bclim",
                "timg/butji_n.bclim",
                "timg/butji_y.bclim",
                "timg/butt_gattai.bclim",
                "timg/butt_jk.bclim",
                "timg/epi_tag_0.bclim",
                "timg/epi_tag_dw0_e.bclim",
                "timg/epi_tag_dw0_ge.bclim",
                "timg/epi_tag_g0.bclim",
                "timg/head_card.bclim",
                "timg/head_epi.bclim",
                "timg/head_map00.bclim",
                "timg/head_map01.bclim",
                "timg/head_map02.bclim",
                "timg/head_map03.bclim",
                "timg/head_map04.bclim",
                "timg/head_piece.bclim",
                "timg/head_tool.bclim",
                "timg/hint_fuki.bclim",
                "timg/je_head_1.bclim",
                "timg/je_head_2.bclim",
                "timg/je_head_3.bclim",
                "timg/je_head_4.bclim",
                "timg/kouko_F.bclim",
                "timg/kouko_moji.bclim",
                "timg/mai_1.bclim",
                "timg/mai_2.bclim",
                "timg/mai_3.bclim",
                "timg/mai_4.bclim",
                "timg/mai_5.bclim",
                "timg/map_01.bclim",
                "timg/map_02.bclim",
                "timg/map_04.bclim",
                "timg/map_mk.bclim",
                "timg/name_00_A.bclim",
                "timg/name_00_B.bclim",
                "timg/name_01_A.bclim",
                "timg/name_01_B.bclim",
                "timg/name_02_A.bclim",
                "timg/name_03_A.bclim",
                "timg/name_03_B.bclim",
                "timg/name_04_A.bclim",
                "timg/name_04_B.bclim",
                "timg/name_05_A.bclim",
                "timg/name_05_B.bclim",
                "timg/name_06_A.bclim",
                "timg/name_06_B.bclim",
                "timg/name_07_A.bclim",
                "timg/name_07_B.bclim",
                "timg/name_08_A.bclim",
                "timg/name_08_B.bclim",
                "timg/name_09_A.bclim",
                "timg/name_09_B.bclim",
                "timg/name_10_A.bclim",
                "timg/name_11_A.bclim",
                "timg/name_11_B.bclim",
                "timg/name_12_A.bclim",
                "timg/name_12_B.bclim",
                "timg/name_13_A.bclim",
                "timg/name_13_B.bclim",
                "timg/name_14_A.bclim",
                "timg/name_14_B.bclim",
                "timg/name_20_A.bclim",
                "timg/pie_tag_3.bclim",
                "timg/pie_tag_dw2_e.bclim",
                "timg/save_moji.bclim",
                "timg/senko_F.bclim",
                "timg/senko_moji.bclim",
                "timg/slct_moji_gt0.bclim",
                "timg/slct_moji_gt1.bclim",
                "timg/slct_moji_gt2.bclim",
                "timg/slct_moji_gt3.bclim",
                "timg/syobu_fuki.bclim",
                "timg/tub_card.bclim",
                "timg/tub_epi.bclim",
                "timg/tub_piece.bclim",
                "timg/tub_tool.bclim",
                "timg/zenz_mk_a.bclim",
            };
            ExtractBclimImages(subsceen0, subscreen0Images);

            var cardtexImages = new[] {
                "File66.bin",
                "File68.bin",
                "File69.bin",
            };
            ExtractCgfxImages(cardtext, cardtexImages);

            Console.WriteLine("5. Extract 3D textures");
            Console.WriteLine("[WARNING] TODO");
        }

        void ExtractBclimImages(Node root, params string[] children)
        {
            string outDir = Path.Combine(ImageDirectory, root.Name);
            var converter = new ExternalProgramConverter {
                Program = Path.Combine(ToolsDirectory, "bclimtool"),
                Arguments = "-dfp <in> <out>",
            };

            foreach (var child in children) {
                Node childNode = Navigator.SearchFile(root, $"{root.Path}/{child}");
                string pngPath = Path.Combine(outDir, childNode.Name + ".png");

                childNode.Transform<BinaryFormat, BinaryFormat>(converter)
                    .Stream.WriteTo(pngPath);
            }
        }

        void ExtractCgfxImages(Node root, params string[] children)
        {
            string outDir = Path.Combine(ImageDirectory, root.Name);
            var converter = new ExternalProgramNodeConverter {
                Program = Path.Combine(ToolsDirectory, "txobtool"),
                Arguments = "-efd <in> <out>",
            };

            foreach (var child in children) {
                Node childNode = Navigator.SearchFile(root, $"{root.Path}/{child}");
                converter.OutputDirectory = Path.Combine(outDir, childNode.Name);
                childNode.Transform<BinaryFormat, NodeContainerFormat>(converter);
            }
        }
    }
}
