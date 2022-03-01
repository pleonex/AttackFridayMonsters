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
#addin nuget:?package=Yarhl&version=3.0.0&loaddependencies=true
#addin nuget:?package=Yarhl.Media&version=3.0.0&loaddependencies=true
#addin nuget:?package=YamlDotNet&version=8.1.2&loaddependencies=true
#addin nuget:?package=Serilog&version=2.9.0
#addin nuget:?package=Serilog.Sinks.Console&version=3.1.1
#addin nuget:?package=Serilog.Sinks.ColoredConsole&version=3.0.1

#r "GameData/tools/AttackFridayMonsters.Formats.dll"
#r "GameData/tools/Lemon.dll"

using System.Collections.Generic;
using AttackFridayMonsters.Formats;
using AttackFridayMonsters.Formats.Compression;
using AttackFridayMonsters.Formats.Container;
using AttackFridayMonsters.Formats.Text;
using AttackFridayMonsters.Formats.Text.Code;
using AttackFridayMonsters.Formats.Text.Layout;
using Lemon.Containers.Converters;
using Lemon.Titles;
using Yarhl.IO;
using Yarhl.FileSystem;
using Yarhl.FileFormat;
using Yarhl.Media.Text;
using Serilog;

const string TitleIdUsa = "00040000000E7600";
const string TitleIdEur = "00040000000E7500";

var target = Argument("target", "Default");

public class BuildData
{
    public string Game { get; set; }

    public string ToolsDirectory { get; set; }

    public string OutputDirectory { get; set; }

    public string ImageDirectory { get { return $"{OutputDirectory}/Images"; } }

    public string FontDirectory { get { return $"{OutputDirectory}/Fonts"; } }

    public string TextDirectory { get { return $"{OutputDirectory}/Texts"; } }

    public string ScriptDirectory { get { return $"{TextDirectory}/Scripts"; } }

    public string LayoutDirectory { get { return $"{TextDirectory}/Layouts"; } }

    public string VideoDirectory { get { return $"{OutputDirectory}/Videos"; } }

    public Node Root { get; set; }

    public Node GetNode(string path)
    {
        return Navigator.SearchNode(Root, $"/root/content/program/rom/{path}");
    }
}

Setup<BuildData>(setupContext => {
    var log = new LoggerConfiguration()
        .WriteTo.ColoredConsole(outputTemplate: "{Timestamp:HH:mm} [{SourceContext:l}] {Level}: {Message}{NewLine}{Exception}")
        .CreateLogger();
    Log.Logger = log;

    return new BuildData {
        ToolsDirectory = "GameData/tools",
        Game = Argument("game", "GameData/00040000000E7500 Attack of the Friday Monsters! (CTR-N-JKEP) (E).cia"),
        OutputDirectory = Argument("output", "GameData/extracted"),
    };
});

Task("Open-Game")
    .Does<BuildData>(data =>
{
    if (!FileExists(data.Game)) {
        throw new Exception($"The game file '{data.Game}' does not exist");
    }

    data.Root = NodeFactory.FromFile(data.Game, "root")
        .TransformWith<BinaryCia2NodeContainer>();

    string titleId = data.Root.Children["title"]
        .TransformWith<Binary2TitleMetadata>()
        .GetFormatAs<TitleMetadata>()
        .TitleId.ToString("X16");
    if (titleId != TitleIdUsa && titleId != TitleIdEur) {
        throw new Exception($"Invalid game with title ID: {titleId}");
    }

    var programNode = data.Root.Children["content"].Children["program"];
    if (programNode.Tags.ContainsKey("LEMON_NCCH_ENCRYPTED")) {
        throw new Exception("Encrypted (legit) CIA not supported");
    }

    programNode.TransformWith<Binary2Ncch>();
    programNode.Children["rom"].TransformWith<BinaryIvfc2NodeContainer>();
    programNode.Children["system"].TransformWith<BinaryExeFs2NodeContainer>();
});

Task("Extract-System")
    .IsDependentOn("Open-Game")
    .Does<BuildData>(data =>
{
    Information("Extracting text from code.bin");
    DataStream stringDefinitions = DataStreamFactory.FromFile(
        $"{data.ToolsDirectory}/code_text.yml",
        FileOpenMode.Read);

    // Decompress binary
    var compressionConverter = new ExternalProgramConverter {
       Program = $"{data.ToolsDirectory}/blz",
       Arguments = "-d <inout>",
    };

    Navigator.SearchNode(data.Root, "/root/content/program/system/.code")
        .TransformWith(compressionConverter)
        .TransformWith<BinaryStrings2Po, DataStream>(stringDefinitions)
        .TransformWith<Po2Binary>()
        .Stream.WriteTo($"{data.TextDirectory}/code.po");
});

Task("Unpack")
    .IsDependentOn("Open-Game")
    .Does<BuildData>(data =>
{
    data.GetNode("gkk/lyt/title.arc").TransformWith<DarcToBinary>();
    data.GetNode("gkk/lyt/bumper.arc").TransformWith<DarcToBinary>();
    data.GetNode("gkk/lyt/movie_low.arc").TransformWith<DarcToBinary>();
    data.GetNode("gkk/lyt/notebook.arc").TransformWith<DarcToBinary>();
    data.GetNode("gkk/lyt/selwin.arc").TransformWith<DarcToBinary>();
    data.GetNode("gkk/lyt/sub_screen.bin").TransformWith<Ofs3ToBinary>()
        .Children["File0.bin"].TransformWith<DarcToBinary>();

    data.GetNode("gkk/cardgame/carddata.bin").TransformWith<Ofs3ToBinary>();
    data.GetNode("gkk/cardgame/cardlyt_d.arc").TransformWith<DarcToBinary>();
    data.GetNode("gkk/cardgame/cardtex.bin").TransformWith<Ofs3ToBinary>();

    data.GetNode("gkk/episode/episode.bin").TransformWith<Ofs3ToBinary>();
});

Task("Export-Fonts")
    .IsDependentOn("Unpack")
    .Does<BuildData>(data =>
{
    var scriptPath = MakeAbsolute(File($"{data.ToolsDirectory}/bcfnt.py"));
    var fontConverter = new ExternalProgramNodeConverter {
        Program = "python",
        Arguments = $"{scriptPath} -x -y -f <in>",
        WorkingDirectory = data.FontDirectory,
        WorkingDirectoryAsOutput = true,
        FileName = "kk_KN_Font.bcfnt"
    };

    data.GetNode("gkk/lyt/title.arc/font/kk_KN_Font.bcfnt")
        .TransformWith(fontConverter);
});

Task("Export-Texts")
    .IsDependentOn("Unpack")
    .Does<BuildData>(data =>
{
    Information("Card texts");
    data.GetNode("gkk/cardgame/carddata.bin/File0.bin")
        .TransformWith<CardDataToPo, int>(0)
        .TransformWith<Po2Binary>()
        .Stream.WriteTo($"{data.TextDirectory}/cardinfo.po");

    data.GetNode("gkk/cardgame/carddata.bin/File25.bin")
        .TransformWith<CardDataToPo, int>(25)
        .TransformWith<Po2Binary>()
        .Stream.WriteTo($"{data.TextDirectory}/cardgame_dialogs.po");

    Information("Story chapters");
    data.GetNode("gkk/episode/episode.bin/epsetting.dat")
        .TransformWith<EpisodeSettingsToPo>()
        .TransformWith<Po2Binary>()
        .Stream.WriteTo($"{data.TextDirectory}/episodes_title.po");

    Information("Text from layouts");
    ExportClyt(data, "title", "gkk/lyt/title.arc/blyt/save_load.bclyt");
    ExportClyt(data, "title", "gkk/lyt/title.arc/blyt/sta_menu.bclyt");
    ExportClyt(data, "cardlyt", "gkk/cardgame/cardlyt_d.arc/blyt/kbattle_sita.bclyt");
    ExportClyt(data, "notebook", "gkk/lyt/notebook.arc/blyt/techo_sita.bclyt");
    ExportClyt(data, "notebook", "gkk/lyt/notebook.arc/blyt/techo_ue.bclyt");
    ExportClyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/auto_save.bclyt");
    ExportClyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/epi_titile.bclyt");
    ExportClyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/msgWin_and_cardBattle.bclyt");
    ExportClyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/piece_get.bclyt");
    ExportClyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_card_sita.bclyt");
    ExportClyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_card_ue.bclyt");
    ExportClyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_epi.bclyt");
    ExportClyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_piece.bclyt");
    ExportClyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_tool.bclyt");
    ExportClyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/tool_save.bclyt");

    Information("Scripts");
    var maps = data.GetNode("gkk/map_gz").Children
        .Where(n => n.Name[0] == 'A' || n.Name[0] == 'B');

    foreach (var map in maps) {
        string mapName = map.Name.Substring(0, map.Name.IndexOf("."));
        string scriptFile = $"{data.ScriptDirectory}/{mapName}.po";

        Node script = map
            .TransformWith<Lz11Decompression>()
            .TransformWith<Ofs3ToBinary>()
            .Children["File1.bin"];
        if (script.Stream.Length > 0) {
            script.TransformWith<ScriptToPo>()
                .TransformWith<Po2Binary>()
                .Stream.WriteTo(scriptFile);
        }
    }

    Information("sys_data");
    var sysData = data.GetNode("gkk/sys_data/sys_data.lz")
        .TransformWith<Lz11Decompression>()
        .TransformWith<Ofs3ToBinary>()
        .Children[4]
        .TransformWith<Ofs3ToBinary>();

    sysData.Children[0]
        .TransformWith<ScriptBlock2Po>()
        .TransformWith<Po2Binary>()
        .Stream.WriteTo($"{data.TextDirectory}/sys_data0.po");
    sysData.Children[1]
        .TransformWith<ScriptBlock2Po>()
        .TransformWith<Po2Binary>()
        .Stream.WriteTo($"{data.TextDirectory}/sys_data1.po");
});

void ExportClyt(BuildData data, string group, string path)
{
    Node node = data.GetNode(path).TransformWith<Binary2Clyt>();
    Clyt clyt = node.GetFormatAs<Clyt>();
    string name = node.Name.Replace(".bclyt", string.Empty);

    ((BinaryFormat)ConvertFormat.With<Clyt2Yml>(clyt))
        .Stream.WriteTo($"{data.LayoutDirectory}/{group}/{name}.yml");

    Po po = (Po)ConvertFormat.With<Clyt2Po>(clyt);
    ((BinaryFormat)ConvertFormat.With<Po2Binary>(po))
        .Stream.WriteTo($"{data.LayoutDirectory}/{group}/{name}.po");
}

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
    ExtractBclimImages(data, "cardlyt", "gkk/cardgame/cardlyt_d.arc", cardlytImages);

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
    ExtractBclimImages(data, "sub_screen", "gkk/lyt/sub_screen.bin/File0.bin", subscreen0Images);

    var cardtexImages = new[] {
        "File66.bin",
        "File68.bin",
        "File69.bin",
    };
    ExtractCgfxImages(data, "gkk/cardgame/cardtex.bin", cardtexImages);
});

void ExtractBclimImages(BuildData data, string rootPath, params string[] children)
{
    Node root = data.GetNode(rootPath);
    string rootName = System.IO.Path.GetFileNameWithoutExtension(root.Name);
    ExtractBclimImages(data, rootName, rootPath, children);
}

void ExtractBclimImages(BuildData data, string rootName, string rootPath, params string[] children)
{
    Node root = data.GetNode(rootPath);
    string outDir = $"{data.ImageDirectory}/{rootName}";
    var converter = new ExternalProgramConverter {
        Program = $"{data.ToolsDirectory}/bclimtool",
        Arguments = "-dfp <in> <out>",
    };

    foreach (var child in children) {
        Node childNode = data.GetNode($"{rootPath}/{child}");
        string childName = System.IO.Path.GetFileNameWithoutExtension(childNode.Name);
        string pngPath = $"{outDir}/{childName}.png";

        childNode.TransformWith(converter)
            .Stream.WriteTo(pngPath);
    }
}

void ExtractCgfxImages(BuildData data, string rootPath, params string[] children)
{
    Node root = data.GetNode(rootPath);
    string rootName = System.IO.Path.GetFileNameWithoutExtension(root.Name);
    string outDir = $"{data.ImageDirectory}/{rootName}";
    var converter = new ExternalProgramNodeConverter {
        Program = $"{data.ToolsDirectory}/txobtool",
        Arguments = "-efd <in> <out>",
        OutputDirectory = outDir,
    };

    foreach (var child in children) {
        Node childNode = data.GetNode($"{rootPath}/{child}");
        childNode.TransformWith(converter);
    }
}

Task("Export-Videos")
    .IsDependentOn("Open-Game")
    .Does<BuildData>(data =>
{
    Warning("TODO: Convert to MP4 with Mobius");
    data.GetNode("gkk/movie/opening.moflex")
        .Stream.WriteTo($"{data.VideoDirectory}/opening.moflex");
    data.GetNode("gkk/movie/ending.moflex")
        .Stream.WriteTo($"{data.VideoDirectory}/ending.moflex");
});

Task("Default")
    .IsDependentOn("Extract-System")
    .IsDependentOn("Export-Fonts")
    .IsDependentOn("Export-Texts")
    .IsDependentOn("Export-Images")
    .IsDependentOn("Export-Videos");

RunTarget(target);
