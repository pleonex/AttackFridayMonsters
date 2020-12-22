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
            Title = "Asistente Clippy para parchear";
            Maximizable = false;
            Resizable = false;

            Padding = new Padding(5);
            Content = GetBaseInstructions();
        }

        Control GetCitraInstructions()
        {
            return "¡Juego parcheado e instalado correctamente en Citra!\n" +
                "Asegúrate de actualizar Citra y tener una versión igual o mayor a 1659";
        }

        Control GetBaseInstructions()
        {
            var citraRadioBtn = new RadioButton {
                Text = "Emulador Citra",
                Checked = true
            };

            var consoleRadioBtn = new RadioButton(citraRadioBtn) {
                Text = "Consola",
            };

            var table = new TableLayout {
                Padding = 10,
                Spacing = new Size(10, 10),
                Rows = {
                    new TableRow("1. Compra el juego desde la e-shop de Nintendo."),
                    new TableRow("2. Dumpea el juego en formato CIA a la tarjeta microSD usando godmode9."),
                    new TableRow("3. Copia el juego de la tarjeta microSD al ordenador."),
                    new TableRow("4. Selecciona el fichero CIA del juego:"),
                    new TableLayout {
                        Spacing = new Size(10, 10),
                        Rows = {
                            new TableRow {
                                Cells = {
                                    new Button {
                                        Text = "Seleccionar",
                                    },
                                    new TextBox {
                                        ReadOnly = true,
                                        Text = "/store/Juegos/3DS/00040000000E7500 Attack of the Friday Monsters! (CTR-N-JKEP) (E).cia",
                                        Width = 300,
                                    },
                                },
                            },
                        },
                    },
                    new TableRow(new Label { Text = "Validando el juego...", Font = SystemFonts.Bold() } ),
                    new TableRow("5. Selecciona dónde quieres jugar:"),
                    new TableLayout {
                        Spacing = new Size(10, 10),
                        Rows = {
                            new TableRow(citraRadioBtn),
                            new TableRow(consoleRadioBtn),
                        },
                    },
                    new TableRow() { ScaleHeight = true },
                    new TableLayout {
                        Rows = {
                            new TableRow {
                                Cells = {
                                    new Button {
                                        Text = "¡Parchear!",
                                        Font = SystemFonts.Bold()
                                    },
                                    new TableCell(null, true)
                                },
                            },
                        },
                    },
                    new TableRow(),
                },
            };

            var drawable = new Drawable {
                Content = table,
            };

            drawable.Paint += (sender, e) =>
                e.Graphics.DrawImage(
                    image: Bitmap.FromResource(ResourcesName.Clippy),
                    x: 285,
                    y: 180,
                    width: 218,
                    height: 168);

            return drawable;
        }
    }
}