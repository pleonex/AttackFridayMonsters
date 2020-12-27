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
    using Eto.Forms;
    using Eto.Drawing;
    using Patcher.Resources;
    using Patcher.ViewModels;
    using System.Reflection;

    public sealed class MainForm : Form
    {
        MainViewModel viewModel;

        public MainForm()
        {
            Application.Instance.UnhandledException += (sender, e) =>
                Logger.Log($"CRASH: {e.ExceptionObject}");

            viewModel = new MainViewModel();
            DataContext = viewModel;

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = string.Format(L10n.Get("Fan-translation of Attack of the Friday Monsters ~ v{0}"), version);
            Icon = Icon.FromResource(ResourcesName.Icon);
            Logger.Log($"MainForm - Version: {version}");

            // On GTK3 did doesn't work
            // https://github.com/picoe/Eto/issues/1652
            Maximizable = false;
            Resizable = false;

            var patchBtn = new Button {
                Text = L10n.Get("Patch!", "Main window button"),
                Font = SystemFonts.Bold(),
                Command = viewModel.PatchCommand,
            };

            var creditsBtn = new Button {
                Text = L10n.Get("Credits", "Main window button"),
                Command = viewModel.OpenCreditsCommand,
            };

            var drawable = new Drawable {
                Size = new Size(600, 359),
                Content = new StackLayout {
                    Padding = 10,
                    Spacing = 10,
                    VerticalContentAlignment = VerticalAlignment.Bottom,
                    Orientation = Orientation.Horizontal,
                    Items = { patchBtn, creditsBtn },
                },
            };

            drawable.Paint += (sender, e) =>
                e.Graphics.DrawImage(
                    image: Bitmap.FromResource(ResourcesName.MainBackground),
                    x: 0,
                    y: 0,
                    width: drawable.Width,
                    height: drawable.Height);
            Content = drawable;
        }
    }
}
