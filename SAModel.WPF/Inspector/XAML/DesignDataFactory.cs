using SATools.SAModel.ObjData;
using SATools.SAModel.WPF.Inspector.Viewmodel;
using System;

namespace SATools.SAModel.WPF.Inspector.XAML
{
    internal static class DesignDataFactory
    {
        private class IVmDebugSub : InspectorViewModel
        {
            private class DebugSub { }

            protected override Type ViewmodelType
                => typeof(DebugSub);

            [DisplayName("Text")]
            [Tooltip("The text of a sub class")]
            public string TextValue { get; set; }

            [DisplayName("Number")]
            public int Number { get; set; } = new Random().Next();

            public IVmDebugSub() : base() { }

        }

        /// <summary>
        /// An Inspector viewmodel used for testing
        /// </summary>
        private class IVmDebug : InspectorViewModel
        {
            private class Debug { }

            protected override Type ViewmodelType
                => typeof(Debug);

            [Ignore]
            public int HiddenValue { get; set; }

            #region Strings

            [DisplayName("Text")]
            [Tooltip("A simple, modifiable text")]
            public string TextValue { get; set; }
                = "Reality can be whatever I want";

            [DisplayName("Readonly Text")]
            [Tooltip("This text cannot be edited")]
            public string ReadonlyTextValue { get; }
                = "This value cannot be modified";

            #endregion

            #region Numbers

            [DisplayName("Hexadecimal Integer")]
            [Tooltip("A Signed 32 bit Number represented as a Hexadecimal")]
            [Hexadecimal]
            public int HexInt { get; set; } = 0xBEEF;

            [DisplayName("Hybrid Integer")]
            [Tooltip("A Signed 32 bit Number represented as a Hexadecimal")]
            [Hexadecimal(true)]
            public int HybridHexInt { get; set; } = 0xBEEF;

            [DisplayName("Long")]
            [Tooltip("A Signed 64 bit Number")]
            public long LongValue { get; set; }

            [DisplayName("Unsigned Long")]
            [Tooltip("An Unsigned 64 bit Number")]
            public ulong UnsignedLongValue { get; set; }

            [DisplayName("Integer")]
            [Tooltip("A Signed 32 bit Number")]
            public int IntegerValue { get; set; }

            [DisplayName("Unsigned Integer")]
            [Tooltip("An Unsigned 32 bit Number")]
            public uint UnsignedIntegerValue { get; set; }

            [DisplayName("Short")]
            [Tooltip("A signed 16 bit Number")]
            public short ShortValue { get; set; }

            [DisplayName("Unsigned Short")]
            [Tooltip("An Unsigned 16 bit Number")]
            public ushort UnsignedShortValue { get; set; }

            [DisplayName("Single")]
            [Tooltip("A 32 bit Floating point number")]
            public float SingleValue { get; set; }

            [DisplayName("Double")]
            [Tooltip("A 64 bit Floating point number")]
            public double DoubleValue { get; set; }

            #endregion

            #region Enums

            [DisplayName("Format")]
            public LandtableFormat EnumTest { get; set; }

            [DisplayName("Object Flags")]
            [Tooltip("Object data flags")]
            public ObjectAttributes FlagTest { get; set; } = ObjectAttributes.NoMorph | ObjectAttributes.NoAnimate | ObjectAttributes.NoChildren;

            #endregion

            #region Classes

            [DisplayName("Debug sub class")]
            [Tooltip("Class object")]
            public IVmDebugSub ClassTest { get; set; } = new();

            [DisplayName("Debug sub class (null)")]
            [Tooltip("Null object")]
            public IVmDebugSub NullClassTest { get; set; }

            #endregion

            public IVmDebug() : base() { }
        }

        public static VmInspector Inspector;

        public static InspectorViewModel DebugInspectorViewmodel;

        static DesignDataFactory()
        {
            Inspector = new();
            DebugInspectorViewmodel = new IVmDebug();
        }
    }
}
