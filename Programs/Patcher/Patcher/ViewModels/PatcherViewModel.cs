namespace Patcher.ViewModels
{
    using Microsoft.Toolkit.Mvvm.ComponentModel;

    public class PatcherViewModel : ObservableObject
    {
            // using var openFileDialog = new OpenFileDialog {
            //     CheckFileExists = true,
            //     Filters = {
            //         new FileFilter("CTR Importable Archive (CIA)", "cia"),
            //         new FileFilter(LocalizationManager.OpenFileFilterAll, "*"),
            //     },
            //     MultiSelect = false,
            //     Title = LocalizationManager.OpenPatchFileTitle,
            // };
            // if (openFileDialog.ShowDialog(Application.Instance.MainForm) != DialogResult.Ok) {
            //     return;
            // }

            // string inputFile = openFileDialog.FileName;
            // string inputName = Path.GetFileNameWithoutExtension(inputFile);

            // using var saveFileDialog = new SaveFileDialog {
            //     CheckFileExists = false,
            //     Filters = {
            //         new FileFilter("CTR Importable Archive (CIA)", "cia"),
            //         new FileFilter(LocalizationManager.OpenFileFilterAll, "*"),
            //     },
            //     FileName = inputName + "_patched.cia",
            //     Title = LocalizationManager.OpenOutputTitle,
            // };
            // if (saveFileDialog.ShowDialog(Application.Instance.MainForm) != DialogResult.Ok) {
            //     return;
            // }

            // string outputFile = saveFileDialog.FileName;
    }
}