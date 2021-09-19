using System;

namespace SATools.SAModel.WPF.Inspector.Viewmodel
{
    internal class InvalidInspectorTypeException : Exception
    {
        /// <summary>
        /// The invalid type
        /// </summary>
        public Type Type { get; }

        public InvalidInspectorTypeException(Type type) : base($"No Inspector viewmodel for type \"{type}\" found") => Type = type;
    }
}
