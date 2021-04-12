using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;

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

            Title = $"{Programname} Error";
            Description.Text = errorDescription;
            Log.Text =
                string.Format(logText,
                Programname,
                File.GetLastWriteTimeUtc(Assembly.GetExecutingAssembly().Location).ToString(CultureInfo.InvariantCulture),
                Environment.OSVersion.ToString(),
                log);
        }

        static void UpdateClipboard(object info)
        {
            string text = (string)info;
            Clipboard.SetText(text);
        }

        private void Report_Click(object sender, RoutedEventArgs e)
        {
            Thread newThread = new Thread(new ParameterizedThreadStart(UpdateClipboard));
            newThread.SetApartmentState(ApartmentState.STA);
            newThread.Start(Log.Text);
            System.Diagnostics.Process.Start("https://github.com/X-Hax/SA3D/issues");
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
    }
}
