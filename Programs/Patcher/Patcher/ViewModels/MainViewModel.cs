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
    using System.Windows.Input;
    using Microsoft.Toolkit.Mvvm.ComponentModel;
    using Microsoft.Toolkit.Mvvm.Input;
    using Patcher.Views;

    public class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            PatchCommand = new RelayCommand(Patch);
            OpenCreditsCommand = new RelayCommand(OpenCredits);
        }

        public ICommand PatchCommand { get; private set; }

        public ICommand OpenCreditsCommand { get; private set; }

        private void OpenCredits()
        {
            Logger.Log("Opening credits");
            using var dialog = new CreditsDialog();
            dialog.ShowModal(Eto.Forms.Application.Instance.MainForm);
        }

        private void Patch()
        {
            Logger.Log("Opening patcher");
            using var dialog = new PatcherDialog();
            dialog.ShowModal(Eto.Forms.Application.Instance.MainForm);
        }
    }
}
