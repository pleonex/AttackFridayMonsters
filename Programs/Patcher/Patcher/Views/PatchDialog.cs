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
namespace Patcher.Views
{
    using System;
    using Eto.Drawing;
    using Eto.Forms;
    using Patcher.Patching;
    using Patcher.Resources;
    using Patcher.ViewModels;

    public class PatcherDialog : Dialog
    {
        PatcherViewModel viewModel;

        public PatcherDialog()
        {
            viewModel = new PatcherViewModel();
            DataContext = viewModel;

            InitializeComponents();
        }

        public override void Close()
        {
            viewModel?.Dispose();
            base.Close();
        }

        private void InitializeComponents()
        {
            Title = L10n.Get("Patching assistant Clippy");
            Maximizable = false;
            Resizable = false;

            Padding = new Padding(5);
            this.BindDataContext(
                s => s.Content,
                Binding.Property((PatcherViewModel vm) => vm.PatchScene)
                    .Convert(scene => GetControlFromScene(scene)));
        }

        Control GetControlFromScene(PatchScene scene) =>
            scene switch {
                PatchScene.BaseInstructions => GetBaseInstructions(),
                PatchScene.CitraInstructions => GetCitraInstructions(),
                PatchScene.ConsoleInstructions => GetConsoleInstructions(),
                _ => throw new InvalidOperationException("Invalid transition"),
            };

        Control GetCitraInstructions()
        {
            return new Label { Text = L10n.Get(
                "Game successfully patched! Just open the game in Citra to play.\n" +
                "Make sure you are using Citra version 1659 or higher.") };
        }

        Control GetConsoleInstructions()
        {
            return new Label { Text = L10n.Get(
                "Copy the new folder Luma to the root directory of your microSD\n" +
                "and start the game as always!") };
        }

        Control GetBaseInstructions()
        {
            string GetGameStatusText(FilePatchStatus status) =>
                status switch {
                    FilePatchStatus.Unknown => L10n.Get("Oops, error?", "File patch status"),
                    FilePatchStatus.NoFile => L10n.Get("No file selected"),
                    FilePatchStatus.ValidFile => L10n.Get("Game compatible with the patch!"),
                    FilePatchStatus.InvalidFormat => L10n.Get("This is not a game in CIA format"),
                    FilePatchStatus.InvalidRegion => L10n.Get("The patcher does not support this game region"),
                    FilePatchStatus.InvalidTitle => L10n.Get("Invalid game..."),
                    FilePatchStatus.InvalidVersion => L10n.Get("The patcher does not support this game version"),
                    FilePatchStatus.InvalidDump => L10n.Get("Game dump is not valid. Make sure to dump your own game"),
                    FilePatchStatus.GameIsEncrypted => L10n.Get("Game is encrypted. Redump the game decrypted"),
                    _ => L10n.Get("Oops, error?", "File patch status"),
                };

            var selectGameBtn = new Button {
                Text = L10n.Get("Select", "Choose button in patcher"),
                Command = viewModel.SelectGameCommand,
            };

            var selectedPathBox = new TextBox {
                ReadOnly = true,
                PlaceholderText = L10n.Get("Click in the button Select"),
                Width = 300,
            };
            selectedPathBox.TextBinding.BindDataContext<PatcherViewModel>(vm => vm.SelectedGamePath);

            var verifySpinning = new Spinner { Enabled = true };
            verifySpinning.BindDataContext(s => s.Visible, (PatcherViewModel vm) => vm.SelectGameCommand.IsRunning);

            var verifyLabel = new Label {
                Text = string.Empty,
                Font = SystemFonts.Bold(),
            };
            verifyLabel.TextBinding.Bind(
                Binding.Property(viewModel, vm => vm.SelectGameCommand.IsRunning)
                    .Convert(r => r
                        ? L10n.Get("Please wait while we check the game...")
                        : GetGameStatusText(viewModel.FileStatus)));
            verifyLabel.Bind(
                l => l.TextColor,
                Binding.Property(viewModel, vm => vm.FileStatus)
                    .Convert(s => s == FilePatchStatus.ValidFile
                        ? Colors.Green
                        : Colors.Red));

            var citraRadioBtn = new RadioButton {
                Text = L10n.Get("Citra emulator"),
                Checked = true
            };

            var consoleRadioBtn = new RadioButton(citraRadioBtn) {
                Text = L10n.Get("Console"),
            };

            var patchBtn = new Button {
                Text = L10n.Get("Patch!", "Button in patcher"),
                Font = SystemFonts.Bold(),
                Command = viewModel.PatchCommand,
            };

            var table = new TableLayout {
                Padding = 10,
                Spacing = new Size(10, 10),
                Rows = {
                    new TableRow(L10n.Get("1. Buy the game in the Nintendo e-shop")),
                    new TableRow(L10n.Get("2. Dump the game to the microSD using the format no legit CIA\n   (e.g. using godmode9)")),
                    new TableRow(L10n.Get("3. Copy the game from the microSD to the computer.")),
                    new TableRow(L10n.Get("4. Choose the CIA file:")),
                    new TableLayout {
                        Spacing = new Size(10, 10),
                        Rows = { new TableRow(selectGameBtn, selectedPathBox) },
                    },
                    new TableLayout {
                        Spacing = new Size(10, 10),
                        Rows = { new TableRow(verifySpinning, verifyLabel) },
                    },
                    new TableRow(L10n.Get("5. Select how you will play the game:")),
                    new TableLayout {
                        Spacing = new Size(10, 10),
                        Rows = {
                            new TableRow(citraRadioBtn, null),
                            new TableRow(consoleRadioBtn, null),
                        },
                    },
                    new TableRow() { ScaleHeight = true },
                    new TableLayout(new TableRow(patchBtn, null)),
                    new TableRow(),
                },
            };

            var drawable = new Drawable {
                Content = table,
            };

            Bitmap clippyImage = Bitmap.FromResource(ResourcesName.Clippy);
            drawable.Paint += (sender, e) =>
                e.Graphics.DrawImage(
                    image: clippyImage,
                    x: table.Width - clippyImage.Width - 10,
                    y: 200,
                    width: clippyImage.Width,
                    height: clippyImage.Height);

            return drawable;
        }
    }
}
