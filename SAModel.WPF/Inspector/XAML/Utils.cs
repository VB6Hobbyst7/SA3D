using SAModel.WPF.Inspector.Viewmodel;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.Generic;

namespace SAModel.WPF.Inspector.XAML
{
    internal class InspectorElementTemplateSelector : DataTemplateSelector
    {
        public static InspectorElementTemplateSelector Selector { get; } = new();

        private readonly ResourceDictionary _resources;

        private readonly DataTemplate Empty;

        private readonly DataTemplate DetailButton;

        private readonly DataTemplate Hex;

        private readonly DataTemplate HybridHex;

        private readonly Dictionary<Type, DataTemplate> _templates;

        private readonly Dictionary<Type, DataTemplate> _hexTemplates;

        public InspectorElementTemplateSelector()
        {
            _resources = new() { Source = new("/SAModel.WPF;component/Inspector/XAML/RdInspectorTemplates.xaml", UriKind.RelativeOrAbsolute) };

            Empty = (DataTemplate)_resources["/"];
            DetailButton = (DataTemplate)_resources["OpenButton"];
            Hex = (DataTemplate)_resources["OnlyHex"];
            HybridHex = (DataTemplate)_resources["HybridHex"];

            _templates = new();
            _hexTemplates = new();

            foreach(string k in _resources.Keys)
            {
                if(_resources[k] is not DataTemplate t || t.DataType == null)
                    continue;

                Type type = (Type)t.DataType;
                if(k.StartsWith("Hex:"))
                    _hexTemplates.Add(type, t);
                else
                    _templates.Add(type, t);
            }
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if(item == null)
                return Empty;

            IInspectorInfo info = (IInspectorInfo)item;

            Type type = info.ValueType;
            if(!_resources.Contains(type.Name)
                && ((type.IsClass && type != typeof(string))
                    || (type.IsValueType && !type.IsEnum && !type.IsPrimitive)))
            {
                return DetailButton;
            }

            string containerName = ((FrameworkElement)container).Name;
            if(info.Hexadecimal != HexadecimalMode.NoHex 
                && containerName != "NoHex")
            {
                if(containerName == "Hex")
                    return _hexTemplates[type];

                if(info.Hexadecimal == HexadecimalMode.HybridHex)
                    return HybridHex;

                if(info.Hexadecimal == HexadecimalMode.OnlyHex)
                    return Hex;
            }

            return _templates[type];
        }
    }

    internal class InspectorTypeTemplateSelector : DataTemplateSelector
    {
        public static InspectorTypeTemplateSelector Selector { get; } = new();

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            string templateName = item != null && item.GetType().IsGenericType && item.GetType().GetGenericTypeDefinition() == typeof(ListInspectorViewModel<>) 
                ? "ListTemplate" : "GridTemplate";

            return ((FrameworkElement)container).FindResource(templateName) as DataTemplate;
        }
    }

    internal class InspectorBinding : DependencyObject
    {
        public static readonly DependencyProperty PropNameProperty
            = DependencyProperty.RegisterAttached(
                "PropName",
                typeof(string),
                typeof(InspectorBinding),
                new PropertyMetadata(null, new PropertyChangedCallback(UpdatePropName))
                );

        public static string GetPropName(DependencyObject d)
            => (string)d.GetValue(PropNameProperty);

        public static void SetPropName(DependencyObject d, string value)
            => d.SetValue(PropNameProperty, value);

        private static void UpdatePropName(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(e.NewValue == null)
                return;

            DependencyProperty dp = null;
            for(Type t = d.GetType(); dp == null; t = t.BaseType)
            {
                FieldInfo field = t.GetField((string)e.NewValue + "Property");
                dp = (DependencyProperty)field?.GetValue(d);
            }

            Binding binding = new();
            IInspectorInfo element = (IInspectorInfo)((FrameworkElement)d).DataContext;

            binding.Path = new(element.BindingPath);
            binding.Mode = element.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            BindingOperations.SetBinding(d, dp, binding);
        }
    }
}
