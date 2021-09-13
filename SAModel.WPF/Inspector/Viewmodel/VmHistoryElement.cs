namespace SAModel.WPF.Inspector.Viewmodel
{
    /// <summary>
    /// A single history element
    /// </summary>
    internal struct VmHistoryElement
    {
        /// <summary>
        /// Name of the history element
        /// </summary>
        public string HistoryName { get; }

        /// <summary>
        /// Object that contains the data
        /// </summary>
        public object Data { get; }

        public VmHistoryElement(string historyName, object data)
        {
            HistoryName = historyName;
            Data = data;
        }
    }
}
