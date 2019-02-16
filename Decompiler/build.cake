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
#addin nuget:?package=Yarhl&version=2.0.0
#addin nuget:?package=Yarhl.Media&version=2.0.0
#r "../Programs/AttackFridayMonsters/AttackFridayMonsters.Formats/bin/Debug/netstandard2.0/AttackFridayMonsters.Formats.dll"
#r "../../Lemon/src/Lemon/bin/Debug/netstandard2.0/Lemon.dll"

using System.Collections.Generic;
using AttackFridayMonsters.Formats;
using AttackFridayMonsters.Formats.Container;
using AttackFridayMonsters.Formats.Text;
using Lemon.Containers;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.Media.Text;

var target = Argument("target", "Default");

public class BuildData
{
    public string Game { get; set; }

    public string ToolsDirectory { get; set; }

    public string OutputDirectory { get; set; }

    public string InternalDirectory { get { return $"{OutputDirectory}/internal"; } }

    public string ImageDirectory { get { return $"{OutputDirectory}/images"; } }

    public string FontDirectory { get { return $"{OutputDirectory}/fonts"; } }

    public string TextDirectory { get { return $"{OutputDirectory}/texts"; } }

    public string ScriptDirectory { get { return $"{TextDirectory}/scripts"; } }

    public Node Root { get; set; }

    public Node GetNode(string path)
    {
        return Navigator.SearchNode(Root, $"/root/program/rom/{path}");
    }
}

Setup<BuildData>(setupContext => {
    return new BuildData {
        Game = Argument("game", "game.3ds"),
        ToolsDirectory = Argument("tools", "tools"),
        OutputDirectory = Argument("output", "extracted"),
    };
});

Task("Extract-Game")
    .Does<BuildData>(data =>
{
    data.Root = NodeFactory.FromFile(data.Game, "root");
    ContainerManager.Unpack3DSNode(data.Root);
    if (data.Root.Children.Count == 0) {
        throw new Exception("Game folder is empty!");
    }

    Warning("TODO: Extract manual");

    Navigator.SearchNode(data.Root, "/root/program/system/.code")
        .Stream.WriteTo($"{data.InternalDirectory}/code.bin");
    Waring("TODO: Extract logo and game names");
});

Task("Unpack")
    .IsDependentOn("Extract-Game")
    .Does<BuildData>(data =>
{
    data.GetNode("gkk/lyt/title.arc").Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();
    data.GetNode("gkk/lyt/bumper.arc").Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();
    data.GetNode("gkk/lyt/movie_low.arc").Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();
    data.GetNode("gkk/lyt/notebook.arc").Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();
    data.GetNode("gkk/lyt/selwin.arc").Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();
    data.GetNode("gkk/lyt/sub_screen.bin").Transform<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>()
        .Children["File0.bin"].Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();

    data.GetNode("gkk/cardgame/carddata.bin").Transform<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>();
    data.GetNode("gkk/cardgame/cardlyt_d.arc").Transform<DarcToBinary, BinaryFormat, NodeContainerFormat>();
    data.GetNode("gkk/cardgame/cardtex.bin").Transform<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>();

    data.GetNode("gkk/episode/episode.bin").Transform<Ofs3ToBinary, BinaryFormat, NodeContainerFormat>();
});

Task("Export-Fonts")
    .IsDependentOn("Unpack")
    .Does<BuildData>(data =>
{
    var fontConverter = new ExternalProgramNodeConverter {
        Program = "python",
        Arguments = $"{data.ToolsDirectory}/bcfnt.py -x -y -f <in>",
        WorkingDirectory = data.FontDirectory,
        WorkingDirectoryAsOutput = true
    };

    data.GetNode("gkk/lyt/title.arc/font/kk_KN_Font.bcfnt")
        .Transform<BinaryFormat, NodeContainerFormat>(fontConverter);
});

Task("Export-Texts")
    .IsDependentOn("Unpack")
    .Does<BuildData>(data =>
{
    Information("Card texts");
    data.GetNode("gkk/cardgame/carddata.bin/File0.bin")
        .Transform<BinaryFormat, Po>(CardDataToPo.CreateForId(0))
        .Transform<Po2Binary, Po, BinaryFormat>()
        .Stream.WriteTo($"{data.TextDirectory}/cardinfo.po");

    data.GetNode("gkk/cardgame/carddata.bin/File25.bin")
        .Transform<BinaryFormat, Po>(CardDataToPo.CreateForId(25))
        .Transform<Po2Binary, Po, BinaryFormat>()
        .Stream.WriteTo($"{data.TextDirectory}/cardgame_dialogs.po");

    Information("Story chapters");
    data.GetNode("gkk/episode/episode.bin/epsetting.dat")
        .Transform<EpisodeSettingsToPo, BinaryFormat, Po>()
        .Transform<Po2Binary, Po, BinaryFormat>()
        .Stream.WriteTo($"{data.TextDirectory}/episodes_title.po");

    Information("Scripts");
    var compressionConverter = new ExternalProgramConverter {
        Program = $"{data.ToolsDirectory}/lzx",
        Arguments = "-d <inout>",
    };

    var maps = data.GetNode("gkk/map_gz").Children
        .Where(n => n.Name[0] == 'A' || n.Name[0] == 'B');

    foreach (var map in maps) {
        string mapName = map.Name.Substring(0, map.Name.IndexOf("."));
        string scriptFile = $"{data.ScriptDirectory}/{mapName}.po";

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

    Warning("TODO: Text from code");
    Warning("TODO: Text from bclyt");
});

Task("Export-Images")
    .IsDependentOn("Unpack")
    .Does<BuildData>(data =>
{
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
    ExtractBclimImages(data, "gkk/lyt/title.arc", titleImages);

    var bumperImages = new[] {
        "timg/ex_msg_A.bclim",
        "timg/ex_msg_B.bclim",
        "timg/ex_msg_C.bclim",
        "timg/stereo_ji.bclim",
    };
    ExtractBclimImages(data, "gkk/lyt/bumper.arc", bumperImages);

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
    ExtractBclimImages(data, "gkk/cardgame/cardlyt_d.arc", cardlytImages);

    var moviewLowImages = new[] {
        "timg/skip_ji.bclim",
    };
    ExtractBclimImages(data, "gkk/lyt/movie_low.arc", moviewLowImages);

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
    ExtractBclimImages(data, "gkk/lyt/notebook.arc", notebookImages);

    var selwinImages = new[] {
        "timg/butji_n.bclim",
        "timg/butji_y.bclim",
    };
    ExtractBclimImages(data, "gkk/lyt/selwin.arc", selwinImages);

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
    ExtractBclimImages(data, "gkk/lyt/sub_screen.bin/File0.bin", subscreen0Images);

    var cardtexImages = new[] {
        "File66.bin",
        "File68.bin",
        "File69.bin",
    };
    ExtractCgfxImages(data, "gkk/cardgame/cardtex.bin", cardtexImages);

    Warning("TODO: 3D textures");
});

void ExtractBclimImages(BuildData data, string rootPath, params string[] children)
{
    Node root = data.GetNode(rootPath);
    string outDir = $"{data.ImageDirectory}/{root.Name}";
    var converter = new ExternalProgramConverter {
        Program = $"{data.ToolsDirectory}/bclimtool",
        Arguments = "-dfp <in> <out>",
    };

    foreach (var child in children) {
        Node childNode = data.GetNode($"{rootPath}/{child}");
        string pngPath = $"{outDir}/{childNode.Name}.png";

        childNode.Transform<BinaryFormat, BinaryFormat>(converter)
            .Stream.WriteTo(pngPath);
    }
}

void ExtractCgfxImages(BuildData data, string rootPath, params string[] children)
{
    Node root = data.GetNode(rootPath);
    string outDir = $"{data.ImageDirectory}/{root.Name}";
    var converter = new ExternalProgramNodeConverter {
        Program = $"{data.ToolsDirectory}/txobtool",
        Arguments = "-efd <in> <out>",
    };

    foreach (var child in children) {
        Node childNode = data.GetNode($"{rootPath}/{child}");
        converter.OutputDirectory = $"{outDir}/{childNode.Name}";
        childNode.Transform<BinaryFormat, NodeContainerFormat>(converter);
    }
}

Task("Default")
    .IsDependentOn("Export-Fonts")
    .IsDependentOn("Export-Texts")
    .IsDependentOn("Export-Images");

RunTarget(target);
