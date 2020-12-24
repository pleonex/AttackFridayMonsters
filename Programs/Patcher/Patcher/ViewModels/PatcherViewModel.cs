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
    using System.Threading.Tasks;
    using Microsoft.Toolkit.Mvvm.ComponentModel;
    using Microsoft.Toolkit.Mvvm.Input;
    using Patcher.Patching;
    using Patcher.Resources;
    using Yarhl.FileSystem;

    public class PatcherViewModel : ObservableObject, IDisposable
    {
        PatchScene patchScene;
        string selectedGamePath;
        GameNode game;
        FilePatchStatus fileStatus;

        public PatcherViewModel()
        {
            PatchScene = PatchScene.BaseInstructions;
            SelectGameCommand = new AsyncRelayCommand(SelectAndVerifyGame);
            PatchCommand = new RelayCommand(Patch, () => FileStatus == FilePatchStatus.ValidFile);
            FileStatus = FilePatchStatus.NoFile;
        }

        public PatchScene PatchScene {
            get => patchScene;
            set => SetProperty(ref patchScene, value);
        }

        public string SelectedGamePath {
            get => selectedGamePath;
            private set => SetProperty(ref selectedGamePath, value);
        }

        public FilePatchStatus FileStatus {
            get => fileStatus;
            set {
                Eto.Forms.Application.Instance.Invoke(() => {
                    SetProperty(ref fileStatus, value);
                    PatchCommand.NotifyCanExecuteChanged();
                });
            }
        }

        public bool Disposed {
            get;
            private set;
        }

        public AsyncRelayCommand SelectGameCommand { get; }

        public RelayCommand PatchCommand { get; }

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
                await Task.Run(() => VerifyGame());
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

        private void VerifyGame()
        {
            try {
                Logger.Log($"Was game node null? {game == null}");
                game?.Root.Dispose();

                var node = NodeFactory.FromFile(SelectedGamePath, "root");
                game = new GameNode(node, GamePatcher.Patch);
                FileStatus = GameVerifier.Verify(game);
                Logger.Log($"New status: {FileStatus}");
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }

            // programNode.TransformWith<Binary2Ncch>();
            // programNode.Children["rom"].TransformWith<BinaryIvfc2NodeContainer>();
            // programNode.Children["system"].TransformWith<BinaryExeFs2NodeContainer>();
        }

        private void Patch()
        {
            PatchScene = PatchScene.ConsoleInstructions;
        }

            // using var saveFileDialog = new SaveFileDialog {
            //     CheckFileExists = false,
            //     Filters = {
            //         new FileFilter("CTR Importable Archive (CIA)", "cia"),
            //         new FileFilter(LocalizationManager.OpenFileFilterAll, "*"),
            //     },
            //     FileName = inputName + "_patched.cia",
            //     Title = L10n.Get("Choose the output file"),
            // };
            // if (saveFileDialog.ShowDialog(Application.Instance.MainForm) != DialogResult.Ok) {
            //     return;
            // }

            // string outputFile = saveFileDialog.FileName;
    }
}