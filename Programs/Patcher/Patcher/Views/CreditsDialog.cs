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

    public class CreditsDialog : Dialog
    {
        public CreditsDialog()
        {
            Title = LocalizationManager.CreditsWindowTitle;
            Maximizable = false;
            Resizable = false;

            // Content = drawable;
            Content = new ImageView {
                Image = Bitmap.FromResource(ResourcesName.CreditsBackground),
                Size = new Size(1132 / 2, 667 / 2),
            };
        }
    }
}