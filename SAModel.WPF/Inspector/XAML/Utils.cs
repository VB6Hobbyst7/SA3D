﻿using SAModel.WPF.Inspector.Viewmodel;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SAModel.WPF.Inspector.XAML
{
    internal class InspectorElementTemplateSelector : DataTemplateSelector
    {
        public static InspectorElementTemplateSelector Selector { get; } = new();

        private readonly ResourceDictionary _templates
            = new ResourceDictionary { Source = new("/SAModel.WPF;component/Inspector/XAML/RdInspectorTemplates.xaml", UriKind.RelativeOrAbsolute) };

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if(item == null)
                return (DataTemplate)_templates["/"];

            IInspectorInfo info = (IInspectorInfo)item;

            Type type = info.ValueType;
            if(!_templates.Contains(type.Name)
                && ((type.IsClass && type != typeof(string))
                    || (type.IsValueType && !type.IsEnum && !type.IsPrimitive)))
            {
                return (DataTemplate)_templates["OpenButton"];
            }

            string typeName = type.Name;
            if(info.Hexadecimal != HexadecimalMode.NoHex)
            {
                FrameworkElement fe = (FrameworkElement)container;
                if(fe.Name == "Hex")
                    typeName = "Hex:" + typeName;
                else if(fe.Name != "NoHex")
                    typeName = info.Hexadecimal.ToString();
            }

            return (DataTemplate)_templates[typeName];
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
