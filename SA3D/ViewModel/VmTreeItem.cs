using SATools.SA3D.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SATools.SA3D.ViewModel
{
    public enum TreeItemType
    {
        Object,
        ModelHead,
        Model,
        AnimationHead,
        Animation,

        VisualHead,
        Visual,
        CollisionHead,
        Collision,
        AttachHead,
        Attach,

        TextureHead,
        Texture
    }

    [ValueConversion(typeof(TreeItemType), typeof(string))]
    public class TreeItemTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "/SA3D;component/Icons/TreeIcons/" + (TreeItemType)value switch
            {
                TreeItemType.ModelHead or TreeItemType.Model => "Model.png",
                TreeItemType.AnimationHead or TreeItemType.Animation => "Animation.png",
                TreeItemType.TextureHead => "Textures.png",
                TreeItemType.Texture => "Texture.png",
                _ => "Object.png",
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class VmTreeItem : BaseViewModel
    {
        private bool _expanded;

        private bool loaded;

        public VmTreeItem Parent { get; }

        /// <summary>
        /// Item that it contains
        /// </summary>
        public ITreeItemData Data { get; }

        public ObservableCollection<VmTreeItem> Children { get; private set; }

        public TreeItemType ItemType => Data.ItemType;

        public string ItemName => Data.ItemName;

        public bool IsExpanded
        {
            get => _expanded;
            set
            {
                if(value && !loaded)
                {
                    Children.Clear();
                    var children = Data.Expand();
                    foreach(var t in children)
                        Children.Add(new(this, t));
                    loaded = true;
                }

                _expanded = value;
            }
        }

        public VmTreeItem(VmTreeItem parent, ITreeItemData data)
        {
            Parent = parent;
            Data = data;
            Children = new();
            if(Data.CanExpand)
                Children.Add(null);
        }
    }
    
}