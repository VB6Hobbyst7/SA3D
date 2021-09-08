using Microsoft.Win32;
using SATools.SA3D.ViewModel;
using SATools.SAModel.ModelData;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SATools.SA3D.XAML.Dialogs
{
    /// <summary>
    /// Interaction logic for WndSave.xaml
    /// </summary>
    public partial class WndSave : Window
    {
        /// <summary>
        /// Dialog passed to the filepath button
        /// </summary>
        private readonly SaveFileDialog _sfd;

        /// <summary>
        /// The attach format to save in
        /// </summary>
        private AttachFormat _format;

        /// <summary>
        /// The current appmode, which determines which file formats are available
        /// </summary>
        private readonly Mode _appMode;

        /// <summary>
        /// The current file extension
        /// </summary>
        private string _currentFileExtension;

        /// <summary>
        /// Path of the selected file
        /// </summary>
        public string Filepath { get; set; }

        /// <summary>
        /// Format to save the data in
        /// </summary>
        public AttachFormat Format { get; private set; }

        /// <summary>
        /// Whether the file is an NJ file
        /// </summary>
        public bool NJ { get; private set; }

        /// <summary>
        /// Whether the output should be optimized
        /// </summary>
        public bool Optimize { get; private set; }



        public WndSave(Mode appMode, string lastFilePath, AttachFormat lastFormat, bool lastNJ, bool lastOptimized)
        {
            if(appMode is not Mode.Level and not Mode.Model)
                throw new NotImplementedException("Only model and level files can be saved atm");

            InitializeComponent();

            _sfd = new();
            _sfd.OverwritePrompt = false;
            FilepathControl.Dialog = _sfd;

            if(!string.IsNullOrWhiteSpace(lastFilePath))
            {
                _sfd.InitialDirectory = Path.GetDirectoryName(lastFilePath);
                Filepath = lastFilePath;
                _format = lastFormat;
                FormatControl.SelectedIndex = (int)lastFormat;
                NJFormatControl.IsChecked = lastNJ;
                OptimizeControl.IsChecked = lastOptimized;
            }
            else
            {
                _format = AttachFormat.Buffer;
            }
            _appMode = appMode;

            switch(appMode)
            {
                case Mode.Model:
                    Title = "Save Model File";
                    break;
                case Mode.Level:
                    Title = "Save Level File";
                    break;
                case Mode.ProjectSA1:
                case Mode.ProjectSA2:
                case Mode.None:
                default:
                    break;
            }

            RefreshFileExtension();
        }

        private void RefreshFileExtension()
        {
            NJFormatControl.IsEnabled = _format != AttachFormat.Buffer && _format != AttachFormat.GC && _appMode == Mode.Model;
            switch(_format)
            {
                case AttachFormat.Buffer:
                    NJFormatControl.IsChecked = false;
                    if(_appMode == Mode.Model)
                    {
                        _currentFileExtension = "bfmdl";
                        _sfd.Filter = "Buffer Model (*.bfmdl)|*.bfmdl";
                    }
                    else if(_appMode == Mode.Level)
                    {
                        _currentFileExtension = "bflvl";
                        _sfd.Filter = "Buffer Level (*.bflvl)|*.bflvl";
                    }
                    break;
                case AttachFormat.BASIC:
                    if(_appMode == Mode.Model)
                    {
                        _sfd.Filter = NJFormatControl.IsChecked == true ?
                            "BASIC Ninja file (*.nj)|*.nj" :
                            "Sonic Adventure 1 Model (*.sa1mdl)|*.sa1mdl";

                        _currentFileExtension = NJFormatControl.IsChecked == true ? "nj" : "sa1mdl";
                    }
                    else if(_appMode == Mode.Level)
                    {
                        _sfd.Filter = "Sonic Adventure 1 Level (*.sa1lvl)|*.sa1lvl";
                        _currentFileExtension = "sa1lvl";
                    }
                    break;
                case AttachFormat.CHUNK:
                    if(_appMode == Mode.Model)
                    {
                        _sfd.Filter = NJFormatControl.IsChecked == true ?
                            "BASIC Ninja file (*.nj)|*.nj" :
                            "Sonic Adventure 2 Model (*.sa2mdl)|*.sa2mdl";

                        _currentFileExtension = NJFormatControl.IsChecked == true ? "nj" : "sa2mdl";
                    }
                    else if(_appMode == Mode.Level)
                    {
                        _currentFileExtension = "sa2lvl";
                        _sfd.Filter = "Sonic Adventure 2 Level (*.sa2lvl)|*.sa2lvl";
                    }
                    break;
                case AttachFormat.GC:
                    NJFormatControl.IsChecked = false;
                    if(_appMode == Mode.Model)
                    {
                        _sfd.Filter = "Sonic Adventure 2 Battle Model (*.sa2bmdl)|*.sa2bmdl";
                        _currentFileExtension = "sa2bmdl";
                    }
                    else if(_appMode == Mode.Level)
                    {
                        _sfd.Filter = "Sonic Adventure 2 Battle Level (*.sa2blvl)|*.sa2blvl";
                        _currentFileExtension = "sa2blvl";
                    }
                    break;
                default:
                    throw new ArgumentException("No valid Attach format was passed");
            }

            Filepath = Path.ChangeExtension(Filepath, _currentFileExtension);
            FilepathControl.FilePath = Filepath;
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(Filepath))
            {
                _ = MessageBox.Show("Please select a path to write to.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if(File.Exists(Filepath))
            {
                MessageBoxResult r = MessageBox.Show($"\"{Filepath}\"already exists.\n Do you want to overwrite it?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if(r is MessageBoxResult.No or MessageBoxResult.Cancel)
                    return;
            }

            Format = _format;
            NJ = NJFormatControl.IsChecked == true;
            Optimize = OptimizeControl.IsChecked == true;
            DialogResult = true;
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Filepath = null;
            DialogResult = null;
            Close();
        }

        private void NJFormat_Click(object sender, RoutedEventArgs e) 
            => RefreshFileExtension();

        private void Format_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox s = (ComboBox)sender;

            AttachFormat newFormat = (AttachFormat)s.SelectedIndex;
            if(_format == newFormat)
                return;

            _format = newFormat;
            RefreshFileExtension();
        }

        private void FilepathControl_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox fp = (TextBox)sender;

            string newPath = fp.Text;
            if(newPath == "")
                return;

            if(string.IsNullOrWhiteSpace(newPath))
            {
                fp.Text = "";
                return;
            }

            string extension = Path.GetExtension(fp.Text);
            if(extension != _currentFileExtension)
            {
                fp.Text = Path.ChangeExtension(fp.Text, _currentFileExtension);
            }
        }
    }
}
