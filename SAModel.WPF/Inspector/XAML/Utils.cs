using SATools.SAModel.WPF.Inspector.Viewmodel;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace SATools.SAModel.WPF.Inspector.XAML
{
    internal class InspectorElementTemplateSelector : DataTemplateSelector
    {
        public static InspectorElementTemplateSelector Selector { get; } = new();

        public ResourceDictionary Resources { get; }

        private readonly DataTemplate Empty;

        private readonly DataTemplate DetailButton;

        private readonly DataTemplate Hex;

        private readonly DataTemplate HybridHex;

        private readonly Dictionary<Type, DataTemplate> _templates;

        private readonly Dictionary<Type, DataTemplate> _hexTemplates;

        public InspectorElementTemplateSelector()
        {
            Resources = new() { Source = new("/SAModel.WPF;component/Inspector/XAML/RdInspectorTemplates.xaml", UriKind.RelativeOrAbsolute) };

            Empty = (DataTemplate)Resources["/"];
            DetailButton = (DataTemplate)Resources["OpenButton"];
            Hex = (DataTemplate)Resources["OnlyHex"];
            HybridHex = (DataTemplate)Resources["HybridHex"];

            Hex.Resources.Add("TemplateSelector", this);
            HybridHex.Resources.Add("TemplateSelector", this);

            _templates = new();
            _hexTemplates = new();

            foreach (string k in Resources.Keys)
            {
                if (Resources[k] is not DataTemplate t || t.DataType == null)
                    continue;

                Type type = (Type)t.DataType;
                if (k.StartsWith("Hex:"))
                    _hexTemplates.Add(type, t);
                else
                    _templates.Add(type, t);
            }
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
                return Empty;

            IInspectorInfo info = (IInspectorInfo)item;

            Type type = info.ValueType;
            if (!_templates.ContainsKey(type)
                && ((type.IsClass && type != typeof(string))
                    || (type.IsValueType && !type.IsEnum && !type.IsPrimitive)))
            {
                return DetailButton;
            }

            string containerName = ((FrameworkElement)container).Name;
            if (info.Hexadecimal != HexadecimalMode.NoHex
                && containerName != "NoHex")
            {
                if (containerName == "Hex")
                    return _hexTemplates[type];

                if (info.Hexadecimal == HexadecimalMode.HybridHex)
                    return HybridHex;

                if (info.Hexadecimal == HexadecimalMode.OnlyHex)
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
            string templateName = "GridTemplate";
            if (item != null && item.GetType().IsGenericType && item.GetType().GetGenericTypeDefinition() == typeof(ListInspectorViewModel<>))
            {
                bool smoothScroll = (bool)item.GetType().GetProperty("SmoothScroll").GetValue(item);
                templateName = smoothScroll ? "SmoothListTemplate" : "ListTemplate";
            }

            return ((FrameworkElement)container).FindResource(templateName) as DataTemplate;
        }
    }

    internal static class InspectorBinding
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
            if (e.NewValue == null)
                return;

            DependencyProperty dp = null;
            for (Type t = d.GetType(); dp == null; t = t.BaseType)
            {
                FieldInfo field = t.GetField((string)e.NewValue + "Property");
                dp = (DependencyProperty)field?.GetValue(d);
            }

            Binding binding = new();
            if (((FrameworkElement)d).DataContext is not IInspectorInfo element)
                return;

            binding.Path = new(element.BindingPath);
            binding.Mode = element.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            if (!element.IsReadOnly)
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            BindingOperations.SetBinding(d, dp, binding);
        }
    }

    internal static class InputBindingsManager
    {

        public static readonly DependencyProperty UpdatePropertySourceWhenEnterPressedProperty
            = DependencyProperty.RegisterAttached(
                "UpdatePropertySourceWhenEnterPressed",
                typeof(DependencyProperty),
                typeof(InputBindingsManager),
                new(null, OnUpdatePropertySourceWhenEnterPressedPropertyChanged));

        static InputBindingsManager() { }

        public static void SetUpdatePropertySourceWhenEnterPressed(DependencyObject dp, DependencyProperty value)
            => dp.SetValue(UpdatePropertySourceWhenEnterPressedProperty, value);

        public static DependencyProperty GetUpdatePropertySourceWhenEnterPressed(DependencyObject dp)
            => (DependencyProperty)dp.GetValue(UpdatePropertySourceWhenEnterPressedProperty);

        private static void OnUpdatePropertySourceWhenEnterPressedPropertyChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            if (dp is not UIElement element)
                return;

            if (e.OldValue != null)
                element.PreviewKeyDown -= HandlePreviewKeyDown;

            if (e.NewValue != null)
                element.PreviewKeyDown += new KeyEventHandler(HandlePreviewKeyDown);
        }

        static void HandlePreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                DoUpdateSource(e.Source);
        }

        static void DoUpdateSource(object source)
        {
            DependencyProperty property =
                GetUpdatePropertySourceWhenEnterPressed(source as DependencyObject);

            if (property == null || source is not UIElement element)
                return;

            BindingOperations.GetBindingExpression(element, property)?.UpdateSource();
        }
    }
}
