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
namespace Patcher.ViewModels
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Toolkit.Mvvm.ComponentModel;
    using Microsoft.Toolkit.Mvvm.Input;
    using Patcher.Patching;
    using Patcher.Resources;
    using Yarhl.FileSystem;

    public class PatcherViewModel : ObservableObject, IDisposable
    {
        static readonly GamePatch gamePatch = LoadPatchInfo();
        GameNode game;

        PatchScene patchScene;
        string selectedGamePath;
        string selectedOutputPath;
        FilePatchStatus fileStatus;
        double patchProgress;
        TargetDevice targetDevice;

        public PatcherViewModel()
        {
            SelectGameCommand = new AsyncRelayCommand(SelectAndVerifyGame);
            PatchCommand = new AsyncRelayCommand(PatchAsync, () => CanPatch);

            TargetDevice = TargetDevice.ConsoleLayeredFs;
            FileStatus = FilePatchStatus.NoFile;
            PatchScene = PatchScene.BaseInstructions;
        }

        public PatchScene PatchScene {
            get => patchScene;
            private set => Eto.Forms.Application.Instance.Invoke(() => SetProperty(ref patchScene, value));
        }

        public string SelectedGamePath {
            get => selectedGamePath;
            private set => SetProperty(ref selectedGamePath, value);
        }

        public string SelectedOutputPath {
            get => selectedOutputPath;
            private set {
                SetProperty(ref selectedOutputPath, value);
                PatchCommand.NotifyCanExecuteChanged();
            }
        }

        public FilePatchStatus FileStatus {
            get => fileStatus;
            private set => Eto.Forms.Application.Instance.Invoke(() => {
                SetProperty(ref fileStatus, value);
                PatchCommand.NotifyCanExecuteChanged();
            });
        }

        public double PatchProgress {
            get => patchProgress;
            private set => Eto.Forms.Application.Instance.InvokeAsync(() => {
                SetProperty(ref patchProgress, value);
            });
        }

        public TargetDevice TargetDevice {
            get => targetDevice;
            set {
                SetProperty(ref targetDevice, value);
                PatchCommand.NotifyCanExecuteChanged();
            }
        }

        public bool Disposed {
            get;
            private set;
        }

        public bool CanPatch => (FileStatus == FilePatchStatus.ValidFile) &&
            ((TargetDevice == TargetDevice.CitraPcLayeredFs) || !string.IsNullOrEmpty(SelectedOutputPath));

        public AsyncRelayCommand SelectGameCommand { get; }

        public AsyncRelayCommand PatchCommand { get; }

        public void Dispose()
        {
            if (Disposed) {
                return;
            }

            Disposed = true;
            game?.Root.Dispose();
        }

        private async Task SelectAndVerifyGame()
        {
            if (SelectGame()) {
                Logger.Log($"Selected game: {SelectedGamePath}");
                FileStatus = FilePatchStatus.Unknown;
                await VerifyGameAsync();
            }
        }

        private bool SelectGame()
        {
            using var openFileDialog = new Eto.Forms.OpenFileDialog {
                CheckFileExists = true,
                Filters = {
                    new Eto.Forms.FileFilter("CTR Importable Archive (CIA)", "cia"),
                    new Eto.Forms.FileFilter(L10n.Get("All files"), "*"),
                },
                MultiSelect = false,
                Title = L10n.Get("Select the game dump (CIA)"),
            };

            var result = openFileDialog.ShowDialog(Eto.Forms.Application.Instance.MainForm);
            if (result != Eto.Forms.DialogResult.Ok) {
                Logger.Log($"Cancel open dialog: {result}");
                return false;
            }

            SelectedGamePath = openFileDialog.FileName;
            return true;
        }

        private async Task VerifyGameAsync()
        {
            try {
                Logger.Log($"Was game node null? {game == null}");
                game?.Root.Dispose();

                var node = NodeFactory.FromFile(SelectedGamePath, "root");
                game = new GameNode(node, gamePatch);

                FileStatus = await GameVerifier.VerifyAsync(game).ConfigureAwait(false);
                Logger.Log($"New status: {FileStatus}");
            } catch (Exception ex) {
                Logger.Log(ex.ToString());
            }
        }

        private async Task PatchAsync()
        {
            PatchScene = PatchScene.Patching;
            try {
                var patcher = new GamePatcher(gamePatch);
                patcher.ProgressChanged += progress => PatchProgress = progress / 2;

                var watch = Stopwatch.StartNew();
                await patcher.PatchAsync(game).ConfigureAwait(false);
                watch.Stop();
                Logger.Log($"Patched in: {watch.Elapsed}");

                var exporter = new GameExporterLayeredFs(game);
                exporter.ProgressChanged += (sender, progress) => PatchProgress = 0.5 + (progress / 2);
                if (TargetDevice == TargetDevice.CitraPcLayeredFs) {
                    await exporter.ExportToCitraAsync().ConfigureAwait(false);
                } else if (TargetDevice == TargetDevice.ConsoleLayeredFs) {
                    await exporter.ExportToDirectoryAsync(SelectedOutputPath).ConfigureAwait(false);
                }
            } catch (Exception ex) {
                Eto.Forms.MessageBox.Show(ex.ToString(), Eto.Forms.MessageBoxType.Error);
                return;
            }

            PatchScene = PatchScene.Close;
            Eto.Forms.Application.Instance.Invoke(() =>
                Eto.Forms.MessageBox.Show(
                    L10n.Get("Game patched and exported correctly!"),
                    Eto.Forms.MessageBoxType.Information));
        }

        private static GamePatch LoadPatchInfo()
        {
            string text;
            var assembly = typeof(GamePatcher).Assembly;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(ResourcesName.Patches))) {
                text = reader.ReadToEnd();
            }

            return System.Text.Json.JsonSerializer.Deserialize<GamePatch>(text);
        }
    }
}