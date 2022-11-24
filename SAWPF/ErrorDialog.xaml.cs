using SATools.SAModel.Graphics;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace SATools.SAWPF
{
    /// <summary>
    /// Interaction logic for ErrorDialog.xaml
    /// </summary>
    public partial class ErrorDialog : Window
    {
        private const string logText = "Program {0}\n Build Date: {1}\n OS Version: {2}\n Log:\n{3}";

        public ErrorDialog(string Programname, string errorDescription, string log)
        {
            InitializeComponent();

            // getting the build date
            Assembly asm = Assembly.GetExecutingAssembly();
            string date = asm.Location;
            if (string.IsNullOrWhiteSpace(asm.Location))
                date = $"{Programname}.exe";
            date = System.IO.Path.Combine(AppContext.BaseDirectory, date);
            if (File.Exists(date))
                date = File.GetLastWriteTimeUtc(date).ToString(CultureInfo.InvariantCulture);
            else
                date = "--/--/---- --:--:--";

            Title = $"{Programname} Error";
            Description.Text = errorDescription;
            Log.Text =
                string.Format(logText,
                Programname,
                date,
                Environment.OSVersion.ToString(),
                log);
        }

        private static void UpdateClipboard(object info)
        {
            string text = (string)info;
            Clipboard.SetText(text);
        }

        private void Report_Click(object sender, RoutedEventArgs e)
        {
            Thread newThread = new(new ParameterizedThreadStart(UpdateClipboard));
            newThread.SetApartmentState(ApartmentState.STA);
            newThread.Start(Log.Text);

            ProcessStartInfo ps = new("https://github.com/X-Hax/SA3D/issues")
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);

            DialogResult = false;
            Close();
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public static void UnhandledException(Exception ex)
        {
            string app = Assembly.GetEntryAssembly().GetName().Name;

            try
            {
                if (ex.InnerException != null && ex.GetType() == typeof(XamlParseException))
                    ex = ex.InnerException;

                string errDesc
                    = $"{app} has crashed with the following error:\n  {ex.GetType().Name}.\n\n" +
                        "If you wish to report a bug, please include the following in your report:";

                if (ex is ShaderException se && se.IntegratedGraphics)
                    errDesc = "An error occured with your rendering hardware! Please do not use integrated graphics. \n\n" + errDesc;

                if (new ErrorDialog(app, errDesc, ex.ToString()).ShowDialog() != true)
                    Application.Current?.Shutdown();
            }
            catch (Exception ex2)
            {
                MessageBox.Show(ex2.ToString(), "SA3D Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);

                string logPath = AppContext.BaseDirectory + $"\\{app}.log";
                File.WriteAllText(logPath, ex.ToString());
                MessageBox.Show("Unhandled Exception " + ex.GetType().Name + "\nLog file has been saved to:\n" + logPath + ".", $"{app} Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current?.Shutdown();
            }
        }
    }
}
