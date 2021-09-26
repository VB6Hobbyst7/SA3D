using System;

namespace SATools.SAModel.WPF.Inspector.Viewmodel
{
    internal enum HexadecimalMode
    {
        /// <summary>
        /// Don't display the number as a hexadecimal
        /// </summary>
        NoHex,

        /// <summary>
        /// Display it as both
        /// </summary>
        HybridHex,

        /// <summary>
        /// Only display as hexadecimal
        /// </summary>
        OnlyHex
    }

    /// <summary>
    /// Inspector accessor interface
    /// </summary>
    internal interface IInspectorInfo
    {
        /// <summary>
        /// Actual value
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// The type of the property
        /// </summary>
        public Type ValueType { get; }

        /// <summary>
        /// Path to correctly bind the value
        /// </summary>
        public string BindingPath { get; }

        /// <summary>
        /// Whether the property points at an Array
        /// </summary>
        public bool IsCollection { get; }

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Name of the button to click
        /// </summary>
        public string DetailName { get; }

        /// <summary>
        /// If the value has detail info, then this is the name chosen for the hierachy upon selection 
        /// </summary>
        public string HistoryName { get; }

        /// <summary>
        /// Property Tooltip
        /// </summary>
        public string Tooltip { get; }

        public bool IsReadOnly { get; }

        /// <summary>
        /// Whether the value should be displayed as a hexadecimal
        /// </summary>
        public HexadecimalMode Hexadecimal { get; }

        /// <summary>
        /// Whether, upon selection, the background should change
        /// </summary>
        public bool SelectBackground { get; }

        public bool SmoothScroll { get; }
    }
}
