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
    using Eto.Drawing;
    using Eto.Forms;
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

        private void InitializeComponents()
        {
            Title = L10n.Get("Patching assistant Clippy");
            Maximizable = false;
            Resizable = false;

            Padding = new Padding(5);
            Content = GetBaseInstructions();
        }

        Control GetCitraInstructions()
        {
            return L10n.Get(
                "Game successfully patched!Game successfully patched!\n" +
                "Make sure you are using Citra version 1659 or higher.");
        }

        Control GetBaseInstructions()
        {
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
                    .Convert(r => r ? "Validando juego" : $"Formato: {viewModel.FileStatus}"));

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
                    y: 180,
                    width: clippyImage.Width,
                    height: clippyImage.Height);

            return drawable;
        }
    }
}
