using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SATools.SA3D.XAML.UserControls
{
    /// <summary>
    /// Interaction logic for UcFilepath.xaml
    /// </summary>
    public partial class UcFilepath : UserControl
    {
        public static readonly DependencyProperty FilepathReadonlyProperty =
            DependencyProperty.Register(
                    nameof(FilepathReadonly),
                    typeof(bool),
                    typeof(UcFilepath)
            );

        public bool FilepathReadonly
        {
            get => (bool)GetValue(FilepathReadonlyProperty);
            set => SetValue(FilepathReadonlyProperty, value);
        }
        public bool NotFilepathReadonly => !FilepathReadonly;


        public static readonly DependencyProperty FilePathProperty =
            DependencyProperty.Register(
                    nameof(FilePath),
                    typeof(string),
                    typeof(UcFilepath)
            );

        public string FilePath
        {
            get => (string)GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        public FileDialog Dialog { get; set; }

        public event TextChangedEventHandler OnTextChanged;

        public UcFilepath()
        {
            InitializeComponent();

            Dialog = new OpenFileDialog()
            {
                Title = "Select file path"
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(Dialog.ShowDialog() != true)
                return;

            FilePath = Dialog.FileName;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) 
            => OnTextChanged?.Invoke(sender, e);
    }
}
