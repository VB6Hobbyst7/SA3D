using SATools.SAModel.ModelData.BASIC;
using System;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.BASIC
{
    internal class IVmStrip : InspectorViewModel
    {
        private static Strip Default = new(0, false);

        protected override Type ViewmodelType
         => typeof(Strip);

        private Strip Strip
        {
            get => Source == null ? Default : (Strip)Source;
            set => Source = value;
        }

        public bool Reversed
        {
            get => Strip.Reversed;
            set
            {
                var s = Strip;
                s.Reversed = value;
                Strip = s;
            }
        }

        public ushort[] Indices
            => Strip.Indices;



        public IVmStrip() : base() { }

        public IVmStrip(object source) : base(source) { }
    }
}
