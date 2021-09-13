using SAModel.WPF.Inspector.Viewmodel;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SAModel.WPF.Inspector.XAML
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UcInspector : UserControl
    {
        /// <summary>
        /// Stores already loaded user controls, so that those don't need to be reloaded
        /// </summary>
        private readonly Dictionary<object, Control> _tableCache;

        /// <summary>
        /// Access to the viewmodel of the inspector
        /// </summary>
        internal VmInspector Inspector
            => (VmInspector)DataContext;

        public string CurrentHistoryName
            => Inspector.ActiveHistoryElement.HistoryName;

        public UcInspector()
        {
            _tableCache = new();
            InitializeComponent();
        }

        /// <summary>
        /// Load an SAModel struct into the inspector
        /// </summary>
        /// <param name="obj"></param>
        public void LoadNewObject(object obj)
        {
            _tableCache.Clear();
            Inspector.LoadNewObject(obj);
        }

        internal void LoadSubObject(object obj, string name)
        {
            Inspector.LoadSubObject(obj, name);
        }
    }
}