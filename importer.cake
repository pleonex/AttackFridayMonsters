//  Copyright (c) 2019 Benito Palacios Sanchez
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
#addin nuget:?package=Yarhl&version=3.0.0-alpha07&loaddependencies=true&prerelease
#addin nuget:?package=Yarhl.Media&version=3.0.0-alpha07&loaddependencies=true&prerelease
#addin nuget:?package=Serilog&version=2.8.0
#addin nuget:?package=Serilog.Sinks.Console&version=3.0.1
#addin nuget:?package=Serilog.Sinks.ColoredConsole&version=3.0.1

#r "Programs/AttackFridayMonsters/AttackFridayMonsters.Formats/bin/Debug/netstandard2.0/AttackFridayMonsters.Formats.dll"
#r "../Lemon/src/Lemon/bin/Debug/netstandard2.0/Lemon.dll"

using System.Collections.Generic;
using AttackFridayMonsters.Formats;
using AttackFridayMonsters.Formats.Compression;
using AttackFridayMonsters.Formats.Container;
using AttackFridayMonsters.Formats.Text;
using Lemon.Containers;
using Lemon.Containers.Converters;
using Yarhl.IO;
using Yarhl.FileSystem;
using Yarhl.Media.Text;
using Serilog;

const string TitleId = "00040000000E7500";
var target = Argument("target", "Default");

public class BuildData
{
    List<string> modifiedNodes = new List<string>();

    public string Game { get; set; }

    public string ToolsDirectory { get; set; }

    public string TranslationDirectory { get; set; }

    public string OutputDirectory { get; set; }

    public string LumaDirectory { get; set; }

    public string InternalDirectory { get { return $"{TranslationDirectory}/Internal"; } }

    public string ImageDirectory { get { return $"{TranslationDirectory}/Images"; } }

    public string FontDirectory { get { return $"{TranslationDirectory}/Fonts"; } }

    public string TextDirectory { get { return $"{TranslationDirectory}/Texts"; } }

    public string ScriptDirectory { get { return $"{TranslationDirectory}/Texts/Scripts"; } }

    public string LayoutDirectory { get { return $"{TranslationDirectory}/Texts/Layouts"; } }

    public Node Root { get; set; }

    public Node GetNode(string path)
    {
        if (!modifiedNodes.Contains(path)) {
            modifiedNodes.Add(path);
        }

        return Navigator.SearchNode(Root, $"/root/program/rom/{path}");
    }

    public void ExportToLuma()
    {
        foreach (var path in modifiedNodes) {
            var node = Navigator.SearchNode(Root, $"/root/program/rom/{path}");
            if (node == null) {
                continue;
            }

            if (node.IsContainer) {
                foreach (var child in node.Children) {
                    child.Stream.WriteTo($"{LumaDirectory}/romfs/{path}/{child.Name}");
                }
            } else {
                node.Stream.WriteTo($"{LumaDirectory}/romfs/{path}");
            }
        }
    }
}

Setup<BuildData>(setupContext => {
    var log = new LoggerConfiguration()
        .WriteTo.ColoredConsole(outputTemplate: "{Timestamp:HH:mm} [{SourceContext:l}] {Level}: {Message}{NewLine}{Exception}")
        .CreateLogger();
    Log.Logger = log;

    return new BuildData {
        Game = Argument("game", "GameData/game.3ds"),
        ToolsDirectory = Argument("tools", "GameData/tools"),
        OutputDirectory = Argument("output", "GameData/output"),
        LumaDirectory = Argument("luma", $"GameData/luma/titles/{TitleId}"),
        TranslationDirectory = Argument("translation", "Spanish/es"),
    };
});

Task("Open-Game")
    .Does<BuildData>(data =>
{
    data.Root = NodeFactory.FromFile(data.Game, "root");
    ContainerManager.Unpack3DSNode(data.Root);
    if (data.Root.Children.Count == 0) {
        throw new Exception("Game folder is empty!");
    }
});

Task("Import-System")
    .IsDependentOn("Open-Game")
    .Does<BuildData>(data =>
{
    Warning("TODO: Import text into code.bin");
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

Task("Import-Font")
    .IsDependentOn("Unpack")
    .Does<BuildData>(data =>
{
    var scriptPath = MakeAbsolute(File($"{data.ToolsDirectory}/bcfnt.py"));
    var fontConverter = new ExternalProgramConverter {
        Program = "python",
        Arguments = $"{scriptPath} -c -y -f <inout>",
        WorkingDirectory = data.FontDirectory,
        FileName = "kk_KN_Font.bcfnt",
    };

    data.GetNode("gkk/lyt/title.arc/font/kk_KN_Font.bcfnt")
        .TransformWith(fontConverter);
    data.GetNode("gkk/lyt/sub_screen.bin/File0.bin/font/kk_KN_Font.bcfnt")
        .TransformWith(fontConverter);
});

Node ChangeStream(BuildData data, string nodePath, string filePath)
{
    var node = data.GetNode(nodePath);
    var newStream = DataStreamFactory.FromFile(filePath, FileOpenMode.Read);
    node.ChangeFormat(new BinaryFormat(newStream));

    return node;
}

Task("Import-Texts")
    .IsDependentOn("Unpack")
    .Does<BuildData>(data =>
{
    Information("Card texts");
    ChangeStream(data, "gkk/cardgame/carddata.bin/File0.bin", $"{data.TextDirectory}/cardinfo.po")
        .TransformWith<Po2Binary>()
        .TransformWith<CardDataToPo, int>(0);

    ChangeStream(data, "gkk/cardgame/carddata.bin/File25.bin", $"{data.TextDirectory}/cardgame_dialogs.po")
        .TransformWith<Po2Binary>()
        .TransformWith<CardDataToPo, int>(25);

    Information("Story chapters");
    Node episodes = data.GetNode("gkk/episode/episode.bin/epsetting.dat");
    DataStream episodesOrig = episodes.Stream;
    DataStream episodesNew = DataStreamFactory.FromFile(
        $"{data.TextDirectory}/episodes_title.po",
        FileOpenMode.Read);

    episodes.ChangeFormat(new BinaryFormat(episodesNew), disposePreviousFormat: false);
    episodes
        .TransformWith<Po2Binary>()
        .TransformWith<EpisodeSettingsToPo, DataStream>(episodesOrig);
    episodesOrig.Dispose();

    Information("Layout");
    ImportBclyt(data, "gkk/lyt/title.arc/blyt/save_load.bclyt", "title/save_load.po");
    ImportBclyt(data, "gkk/lyt/title.arc/blyt/sta_menu.bclyt", "title/sta_menu.po");
    ImportBclyt(data, "gkk/cardgame/cardlyt_d.arc/blyt/kbattle_sita.bclyt", "cardlyt/kbattle_sita.po");
    ImportBclyt(data, "gkk/lyt/notebook.arc/blyt/techo_sita.bclyt", "notebook/techo_sita.po");
    ImportBclyt(data, "gkk/lyt/notebook.arc/blyt/techo_ue.bclyt", "notebook/techo_ue.po");
    ImportBclyt(data, "gkk/lyt/sub_screen.bin/File0.bin/blyt/auto_save.bclyt", "subscreen/auto_save.po");
    ImportBclyt(data, "gkk/lyt/sub_screen.bin/File0.bin/blyt/epi_titile.bclyt", "subscreen/epi_titile.po");
    ImportBclyt(data, "gkk/lyt/sub_screen.bin/File0.bin/blyt/msgWin_and_cardBattle.bclyt", "subscreen/msgWin_and_cardBattle.po");
    ImportBclyt(data, "gkk/lyt/sub_screen.bin/File0.bin/blyt/piece_get.bclyt", "subscreen/piece_get.po");
    ImportBclyt(data, "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_card_sita.bclyt", "subscreen/sub_card_sita.po");
    ImportBclyt(data, "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_card_ue.bclyt", "subscreen/sub_card_ue.po");
    ImportBclyt(data, "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_epi.bclyt", "subscreen/sub_epi.po");
    ImportBclyt(data, "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_piece.bclyt", "subscreen/sub_piece.po");
    ImportBclyt(data, "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_tool.bclyt", "subscreen/sub_tool.po");
    ImportBclyt(data, "gkk/lyt/sub_screen.bin/File0.bin/blyt/tool_save.bclyt", "subscreen/tool_save.po");
});

void ImportBclyt(BuildData data, string node, string poPath)
{
    Node bclyt = data.GetNode(node);
    DataStream bclytOrig = bclyt.Stream;
    DataStream bclytNew = DataStreamFactory.FromFile(
        $"{data.LayoutDirectory}/{poPath}",
        FileOpenMode.Read);

    bclyt.ChangeFormat(new BinaryFormat(bclytNew), disposePreviousFormat: false);
    bclyt
        .TransformWith<Po2Binary>()
        .TransformWith<BclytToPo, DataStream>(bclytOrig);
    bclytOrig.Dispose();
}

Task("Import-Scripts")
    .IsDependentOn("Unpack")
    .Does<BuildData>(data =>
{
    Information("Scripts");
    var compressionConverter = new ExternalProgramConverter {
        Program = $"{data.ToolsDirectory}/lzx",
        Arguments = "-evb <inout>",
    };
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
            var original = script.Stream;
            var modified = DataStreamFactory.FromFile(scriptFile, FileOpenMode.Read);

            Information(scriptFile);
            script.ChangeFormat(new BinaryFormat(modified), false);
            script.TransformWith<Po2Binary>()
                .TransformWith<ScriptToPo, DataStream>(original);
            original.Dispose();
        }

        map.TransformWith<Ofs3ToBinary>().TransformWith(compressionConverter);
    }
});

Task("Import-Images")
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
    ImportBclimImages(data, "gkk/lyt/title.arc", titleImages);

    var bumperImages = new[] {
        "timg/ex_msg_A.bclim",
        "timg/ex_msg_B.bclim",
        "timg/ex_msg_C.bclim",
        "timg/stereo_ji.bclim",
    };
    ImportBclimImages(data, "gkk/lyt/bumper.arc", bumperImages);

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
    ImportBclimImages(data, "cardlyt", "gkk/cardgame/cardlyt_d.arc", cardlytImages);

    var moviewLowImages = new[] {
        "timg/skip_ji.bclim",
    };
    ImportBclimImages(data, "gkk/lyt/movie_low.arc", moviewLowImages);

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
    ImportBclimImages(data, "gkk/lyt/notebook.arc", notebookImages);

    var selwinImages = new[] {
        "timg/butji_n.bclim",
        "timg/butji_y.bclim",
    };
    ImportBclimImages(data, "gkk/lyt/selwin.arc", selwinImages);

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
    ImportBclimImages(data, "sub_screen", "gkk/lyt/sub_screen.bin/File0.bin", subscreen0Images);

    ImportCgfxImages(data, "gkk/cardgame/cardtex.bin/File66.bin", "cardtex", "result_aiko.png");
    ImportCgfxImages(data, "gkk/cardgame/cardtex.bin/File68.bin", "cardtex", "result_kachi.png");
    ImportCgfxImages(data, "gkk/cardgame/cardtex.bin/File69.bin", "cardtex", "result_make.png");
});

void ImportBclimImages(BuildData data, string rootPath, params string[] children)
{
    Node root = data.GetNode(rootPath);
    string rootName = System.IO.Path.GetFileNameWithoutExtension(root.Name);

    ImportBclimImages(data, rootName, rootPath, children);
}

void ImportBclimImages(BuildData data, string rootName, string rootPath, params string[] children)
{
    string outDir = $"{data.ImageDirectory}/{rootName}";

    foreach (var child in children) {
        Node childNode = data.GetNode($"{rootPath}/{child}");
        string name = System.IO.Path.GetFileNameWithoutExtension(childNode.Name);
        string pngPath = System.IO.Path.GetFullPath($"{outDir}/{name}.png");
        var converter = new ExternalProgramConverter {
            Program = $"{data.ToolsDirectory}/bclimtool",
            Arguments = $"-efp <inout> {pngPath}",
        };

        childNode.TransformWith(converter);
    }
}

void ImportCgfxImages(BuildData data, string node, string dirName, string fileName)
{
    string tempInputFolder = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        System.IO.Path.GetRandomFileName());
    System.IO.Directory.CreateDirectory(tempInputFolder);

    string imagePath = System.IO.Path.GetFullPath($"{data.ImageDirectory}/{dirName}/{fileName}");
    string outputPath = System.IO.Path.Combine(tempInputFolder, fileName);
    System.IO.File.Copy(imagePath, outputPath);

    var converter = new ExternalProgramConverter {
        Program = $"{data.ToolsDirectory}/txobtool",
        Arguments = $"-ifd <inout> {tempInputFolder}",
    };

    data.GetNode(node).TransformWith(converter);
    System.IO.Directory.Delete(tempInputFolder, true);
}

Task("Pack")
    .IsDependentOn("Unpack")
    .Does<BuildData>(data =>
{
    data.GetNode("gkk/lyt/title.arc").TransformWith<DarcToBinary>();
    data.GetNode("gkk/lyt/bumper.arc").TransformWith<DarcToBinary>();
    data.GetNode("gkk/lyt/movie_low.arc").TransformWith<DarcToBinary>();
    data.GetNode("gkk/lyt/notebook.arc").TransformWith<DarcToBinary>();
    data.GetNode("gkk/lyt/selwin.arc").TransformWith<DarcToBinary>();

    data.GetNode("gkk/lyt/sub_screen.bin/File0.bin").TransformWith<DarcToBinary>();
    data.GetNode("gkk/lyt/sub_screen.bin").TransformWith<Ofs3ToBinary>();
    data.GetNode("gkk/cardgame/carddata.bin").TransformWith<Ofs3ToBinary>();

    data.GetNode("gkk/cardgame/cardlyt_d.arc").TransformWith<DarcToBinary>();
    data.GetNode("gkk/cardgame/cardtex.bin").TransformWith<Ofs3ToBinary>();

    data.GetNode("gkk/episode/episode.bin").TransformWith<Ofs3ToBinary>();
});

Task("Save-Game")
    .IsDependentOn("Open-Game")
    .Does<BuildData>(data =>
{
    // Generate the luma folder
    data.ExportToLuma();

    // Generate ExeFS and RomFS files because most emulators / CFW support
    // this kind "layered FS". In the future, Lemon may implement NCSD / CIA
    // generation so we could generate them too.
    // Note that Citra is not stable yet to read the files.
    var program = data.Root.Children["program"];
    program.Children["system"]
        .TransformWith<BinaryExeFs2NodeContainer>()
        .Stream.WriteTo($"{data.OutputDirectory}/game.3ds.exefs");
    program.Children["rom"]
        .TransformWith<NodeContainer2BinaryIvfc>()
        .Stream.WriteTo($"{data.OutputDirectory}/game.3ds.romfs");
});

Task("Default")
    .IsDependentOn("Open-Game")
    .IsDependentOn("Import-System")
    .IsDependentOn("Unpack")
    .IsDependentOn("Import-Font")
    .IsDependentOn("Import-Texts")
    .IsDependentOn("Import-Scripts")
    .IsDependentOn("Import-Images")
    .IsDependentOn("Pack")
    .IsDependentOn("Save-Game");

RunTarget(target);
