using SAModel.WPF.Inspector.Viewmodel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SAModel.WPF.Inspector.XAML
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UcInspector : UserControl
    {
        /// <summary>
        /// Access to the viewmodel of the inspector
        /// </summary>
        internal VmInspector Inspector
            => (VmInspector)DataContext;

        public string CurrentHistoryName
            => Inspector.ActiveHistoryElement.HistoryName;

        public UcInspector() 
            => InitializeComponent();

        /// <summary>
        /// Load an SAModel struct into the inspector
        /// </summary>
        /// <param name="obj"></param>
        public void LoadNewObject(object obj)
        {
            try
            {
                Inspector.LoadNewObject(obj);
            }
            catch(InvalidInspectorTypeException e)
            {
                _ = MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        internal void LoadSubObject(IInspectorInfo info)
        {
            try
            {
                Inspector.LoadSubObject(info);
            }
            catch(InvalidInspectorTypeException e)
            {
                _ = MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}