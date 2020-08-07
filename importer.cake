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
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.Media.Text;
using Serilog;

readonly string HomePath = System.Environment.GetEnvironmentVariable("HOME");
readonly string CitraPathWindows = @$"{HomePath}\AppData\Roaming\Citra\load\mods";
readonly string CitraPathUnix = $"{HomePath}/.local/share/citra-emu/load/mods";
readonly string CitraPath = (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
    ? CitraPathWindows
    : CitraPathUnix;

const string TitleIdUsa = "00040000000E7600";
const string TitleIdEur = "00040000000E7500";

var target = Argument("target", "Default");

public class BuildData
{
    List<string> modifiedNodes = new List<string>();

    public string Game { get; set; }

    public TitleMetadata Title { get; set; }

    public string ToolsDirectory { get; set; }

    public string TranslationDirectory { get; set; }

    public string OutputDirectory { get; set; }

    public string LayeredFsDirectory { get; set; }

    public bool CopyToCitra { get; set; }

    public string ImageDirectory { get { return $"{TranslationDirectory}/Images"; } }

    public string FontDirectory { get { return $"{TranslationDirectory}/Fonts"; } }

    public string TextDirectory { get { return $"{TranslationDirectory}/Texts"; } }

    public string ScriptDirectory { get { return $"{TextDirectory}/Scripts"; } }

    public string LayoutDirectory { get { return $"{TextDirectory}/Layouts"; } }

    public string VideoDirectory { get { return $"{TranslationDirectory}/Videos"; } }

    public Node Root { get; set; }

    public Node GetNode(string path)
    {
        if (!modifiedNodes.Contains(path)) {
            modifiedNodes.Add(path);
        }

        return Navigator.SearchNode(Root, $"/root/content/program/rom/{path}");
    }

    public void ExportToLayeredFs(string outputPath)
    {
        outputPath += $"/{Title.TitleId:X16}";

        foreach (var path in modifiedNodes) {
            var node = Navigator.SearchNode(Root, $"/root/content/program/rom/{path}");
            if (node == null) {
                continue;
            }

            if (node.IsContainer) {
                foreach (var child in node.Children) {
                    child.Stream.WriteTo($"{outputPath}/romfs/{path}/{child.Name}");
                }
            } else {
                node.Stream.WriteTo($"{outputPath}/romfs/{path}");
            }
        }

        Navigator.SearchNode(Root, $"/root/content/program/extended_header")
            .Stream.WriteTo($"{outputPath}/exheader.bin");
        Navigator.SearchNode(Root, $"/root/content/program/system/.code")
            .Stream.WriteTo($"{outputPath}/code.bin");
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
        OutputDirectory = Argument("output", "GameData/output"),
        LayeredFsDirectory = Argument("layeredfs", $"GameData/output/luma/titles"),
        CopyToCitra = Argument("copy-citra", true),
        TranslationDirectory = Argument("translation", "Spanish/es"),
    };
});

Task("Open-Game")
    .Does<BuildData>(data =>
{
    if (!FileExists(data.Game)) {
        throw new Exception($"The game file '{data.Game}' does not exist");
    }

    var gameStream = DataStreamFactory.FromFile(data.Game, FileOpenMode.Read);
    data.Root = new Node("root", new BinaryFormat(gameStream))
        .TransformWith<BinaryCia2NodeContainer>();

    data.Title = data.Root.Children["title"]
        .TransformWith<Binary2TitleMetadata>()
        .GetFormatAs<TitleMetadata>();

    string titleId = data.Title.TitleId.ToString("X16");
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

Task("Import-System")
    .IsDependentOn("Open-Game")
    .Does<BuildData>(data =>
{
    // Create a copy to make modifications
    var exHeader = Navigator.SearchNode(data.Root, "/root/content/program/extended_header");
    var newExHeader = new BinaryFormat();
    exHeader.Stream.WriteTo(newExHeader.Stream);
    exHeader.ChangeFormat(newExHeader);

    // Decompress and compress .code
    var decompressConverter = new ExternalProgramConverter {
       Program = $"{data.ToolsDirectory}/blz",
       Arguments = "-d <inout>",
    };

    var po = NodeFactory.FromFile($"{data.TextDirectory}/code.po")
        .TransformWith<Binary2Po>()
        .GetFormatAs<Po>();

    // Safe warnings:
    // 0x00279402 -> 0x0019b46c: pointer to pointer [removed]
    // 0x00279c90 -> 0x00235664: pointer to pointer [removed]
    // 0x0027a03e -> 0x001def7c: pointer to pointer [removed]
    // 0x0027a070 -> 0x0023ec1c: pointer to pointer [removed]
    // 0x0027a090 -> 0x00242d98: pointer to pointer [removed]
    // 0x0027a0b0 -> 0x00242c24: pointer to pointer [removed]
    // 0x0027a0d0 -> 0x00242cf8: pointer to pointer [removed]
    // 0x0029659a -> 0x001a2184: pointer to pointer [removed]
    // 0x002965f0 -> 0x001a22ec: pointer to pointer [removed]
    // 0x002965f0 -> 0x001a2310: pointer to pointer [removed]
    // 0x00296628 -> 0x001a2320: pointer to pointer [removed]
    // 0x00296628 -> 0x001a2344: pointer to pointer [removed]
    // 0x0029665c -> 0x001a26f4: pointer to pointer [removed]
    // 0x00296672 -> 0x001a2974: pointer to pointer [removed]
    // 0x00296672 -> 0x001a2990: pointer to pointer [removed]
    // 0x0029668a -> 0x001a29f4: pointer to pointer [removed]
    // 0x0029668a -> 0x001a2a10: pointer to pointer [removed]
    // 0x002966a4 -> 0x001a2a60: pointer to pointer [removed]
    // 0x002966a4 -> 0x001a2a7c: pointer to pointer [removed]
    // 0x0029683c -> 0x001a1e6c: pointer to pointer [removed]
    Navigator.SearchNode(data.Root, "/root/content/program/system/.code")
        .TransformWith(decompressConverter)
        .TransformWith<Code3dsPoImporter, (Po, DataStream)>((po, exHeader.Stream));

    // Citra doesn't support compressed code.bin, so we leave it decompressed
    // and we update the flag from the extended header
    exHeader.Stream.Position = 0x0D;
    byte flags = exHeader.Stream.ReadByte();
    flags &= 0xFE; // bit0: 1 code.bin is compressed, 0 decompressed
    exHeader.Stream.Position = 0x0D;
    exHeader.Stream.WriteByte(flags);
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
        .TransformWith<Binary2Po>()
        .TransformWith<CardDataToPo, int>(0);

    ChangeStream(data, "gkk/cardgame/carddata.bin/File25.bin", $"{data.TextDirectory}/cardgame_dialogs.po")
        .TransformWith<Binary2Po>()
        .TransformWith<CardDataToPo, int>(25);

    Information("Story chapters");
    Node episodes = data.GetNode("gkk/episode/episode.bin/epsetting.dat");
    DataStream episodesOrig = episodes.Stream;
    DataStream episodesNew = DataStreamFactory.FromFile(
        $"{data.TextDirectory}/episodes_title.po",
        FileOpenMode.Read);

    episodes.ChangeFormat(new BinaryFormat(episodesNew), disposePreviousFormat: false);
    episodes
        .TransformWith<Binary2Po>()
        .TransformWith<EpisodeSettingsToPo, DataStream>(episodesOrig);
    episodesOrig.Dispose();

    Information("Text from layouts");
    ImportBclyt(data, "title", "gkk/lyt/title.arc/blyt/save_load.bclyt");
    ImportBclyt(data, "title", "gkk/lyt/title.arc/blyt/sta_menu.bclyt");
    ImportBclyt(data, "cardlyt", "gkk/cardgame/cardlyt_d.arc/blyt/kbattle_sita.bclyt");
    ImportBclyt(data, "notebook", "gkk/lyt/notebook.arc/blyt/techo_sita.bclyt");
    ImportBclyt(data, "notebook", "gkk/lyt/notebook.arc/blyt/techo_ue.bclyt");
    ImportBclyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/auto_save.bclyt");
    ImportBclyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/epi_titile.bclyt");
    ImportBclyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/msgWin_and_cardBattle.bclyt");
    ImportBclyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/piece_get.bclyt");
    ImportBclyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_card_sita.bclyt");
    ImportBclyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_card_ue.bclyt");
    ImportBclyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_epi.bclyt");
    ImportBclyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_piece.bclyt");
    ImportBclyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/sub_tool.bclyt");
    ImportBclyt(data, "subscreen", "gkk/lyt/sub_screen.bin/File0.bin/blyt/tool_save.bclyt");
});

void ImportBclyt(BuildData data, string group, string path)
{
    Node node = data.GetNode(path).TransformWith<Binary2Clyt>();
    string name = node.Name.Replace(".bclyt", string.Empty);

    using (Node ymlNode = NodeFactory.FromFile($"{data.LayoutDirectory}/{group}/{name}.yml")) {
        node.TransformWith<Yml2Clyt, BinaryFormat>(ymlNode.GetFormatAs<BinaryFormat>());
    }

    using (Node poNode = NodeFactory.FromFile($"{data.LayoutDirectory}/{group}/{name}.po")) {
        var po = poNode.TransformWith<Binary2Po>().GetFormatAs<Po>();
        node.TransformWith<Po2Clyt, Po>(po);
    }

    node.TransformWith<Clyt2Binary>();
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
            script.TransformWith<Binary2Po>()
                .TransformWith<ScriptToPo, DataStream>(original);
            original.Dispose();
        }

        map.TransformWith<Ofs3ToBinary>().TransformWith(compressionConverter);
    }

    Information("sys_data");
    var sysData = data.GetNode("gkk/sys_data/sys_data.lz")
        .TransformWith<Lz11Decompression>()
        .TransformWith<Ofs3ToBinary>()
        .Children[4]
        .TransformWith<Ofs3ToBinary>();

    DataStream originalSys0 = sysData.Children[0].Stream;
    DataStream scriptSys0 = DataStreamFactory.FromFile(
        $"{data.TextDirectory}/sys_data0.po",
        FileOpenMode.Read);
    sysData.Children[0].ChangeFormat(new BinaryFormat(scriptSys0), false);
    sysData.Children[0].TransformWith<Binary2Po>()
        .TransformWith<ScriptBlock2Po, DataStream>(originalSys0);
    originalSys0.Dispose();

    DataStream originalSys1 = sysData.Children[1].Stream;
    DataStream scriptSys1 = DataStreamFactory.FromFile(
        $"{data.TextDirectory}/sys_data1.po",
        FileOpenMode.Read);
    sysData.Children[1].ChangeFormat(new BinaryFormat(scriptSys1), false);
    sysData.Children[1].TransformWith<Binary2Po>()
        .TransformWith<ScriptBlock2Po, DataStream>(originalSys1);
    originalSys1.Dispose();

    sysData.TransformWith<Ofs3ToBinary>();
    data.GetNode("gkk/sys_data/sys_data.lz")
        .TransformWith<Ofs3ToBinary>()
        .TransformWith(compressionConverter);
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

Task("Import-Videos")
    .IsDependentOn("Open-Game")
    .Does<BuildData>(data =>
{
    ChangeStream(data, "gkk/movie/opening.moflex", $"{data.VideoDirectory}/opening.moflex");
    ChangeStream(data, "gkk/movie/ending.moflex", $"{data.VideoDirectory}/ending.moflex");
});

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

Task("Generate-LayeredFs")
    .IsDependentOn("Open-Game")
    .Does<BuildData>(data =>
{
    data.ExportToLayeredFs(data.LayeredFsDirectory);

    if (data.CopyToCitra) {
        data.ExportToLayeredFs(CitraPath);
    }
});

Task("Generate-Patch")
    .IsDependentOn("Open-Game")
    .Does<BuildData>(data =>
{
    // Generate ExeFS and RomFS so we can create XDelta to patch them later.
    var program = data.Root.Children["content"].Children["program"];

    string exeFsPath = $"{data.OutputDirectory}/game.exefs";
    program.Children["system"]
        .TransformWith<BinaryExeFs2NodeContainer>()
        .Stream.WriteTo(exeFsPath);

    string romFsPath = $"{data.OutputDirectory}/game.romfs";
    program.Children["rom"]
        .TransformWith<NodeContainer2BinaryIvfc>()
        .Stream.WriteTo(romFsPath);

    // We modify the extended header so we need to export it.
    // Traditionally, the tools have been putting together the exheader and the
    // access descriptor and they crash if they are not together, so let's do that.
    string exHeaderPath = $"{data.OutputDirectory}/exheader.bin";
    using (var compatibleExHeader = DataStreamFactory.FromFile(exHeaderPath, FileOpenMode.Write)) {
        program.Children["extended_header"].Stream.WriteTo(compatibleExHeader);
        program.Children["access_descriptor"].Stream.WriteTo(compatibleExHeader);
    }

    // Extract the original CXI
    string originalCxi = $"{data.OutputDirectory}/program_original.cxi";
    if (!FileExists(originalCxi)) {
        var gameStream = DataStreamFactory.FromFile(data.Game, FileOpenMode.Read);
        using (Node game = new Node("root", new BinaryFormat(gameStream))) {
            game.TransformWith<BinaryCia2NodeContainer>()
                .Children["content"].Children["program"]
                .Stream.WriteTo(originalCxi);
        }
    }

    // Create the new CXI (Lemon doesn't support it yet so we use 3dstool)
    string romTool = $"{data.ToolsDirectory}/3dstool";
    string plainPath = $"{data.OutputDirectory}/plain.bin";
    string headerPath = $"{data.OutputDirectory}/header.bin";
    int result = 0;
    if (!FileExists(plainPath) || !FileExists(headerPath)) {
        result = StartProcess(
            romTool,
            $"-x -t cxi -f {originalCxi} --plain {plainPath} --header {headerPath}");
        if (result != 0) {
            throw new Exception("Failed to extract files from CXI");
        }
    }

    string modifiedCxi = $"{data.OutputDirectory}/program_modified.cxi";
    result = StartProcess(
        romTool,
        $"-c -t cxi -f {modifiedCxi} --exefs {exeFsPath} --romfs {romFsPath} " +
        $"--exh {exHeaderPath} --plain {plainPath} --header {headerPath}");
    if (result != 0) {
        throw new Exception("Failed to generate new CXI");
    }

    // Create an XDelta patch
    result = StartProcess(
        $"{data.ToolsDirectory}/xdelta3",
        $"-e -S -f -9 -s {originalCxi} {modifiedCxi} {data.OutputDirectory}/patch.xdelta");
    if (result != 0) {
        throw new Exception("Failed to generate patch");
    }
});

Task("Default")
    .IsDependentOn("Open-Game")
    .IsDependentOn("Import-System")
    .IsDependentOn("Unpack")
    .IsDependentOn("Import-Font")
    .IsDependentOn("Import-Texts")
    .IsDependentOn("Import-Scripts")
    .IsDependentOn("Import-Images")
    .IsDependentOn("Import-Videos")
    .IsDependentOn("Pack")
    .IsDependentOn("Generate-LayeredFs")
    .IsDependentOn("Generate-Patch");

RunTarget(target);
