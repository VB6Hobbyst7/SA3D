using System;
using System.Windows.Input;

namespace SATools.SA3D.ViewModel.Base
{
    /// <summary>
    /// A basic command that runs an Action with a specific parameter
    /// </summary>
    public class RelayCommand<ParameterType> : ICommand
    {
        #region Private Members

        /// <summary>
        /// The action to run
        /// </summary>
        private readonly Action<ParameterType> _mAction;

        #endregion

        #region Public Events

        /// <summary>
        /// The event thats fired when the <see cref="CanExecute(object)"/> value has changed
        /// </summary>
        public event EventHandler CanExecuteChanged = (sender, e) => { };

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public RelayCommand(Action<ParameterType> action) => _mAction = action;

        #endregion

        #region Command Methods

        /// <summary>
        /// A relay command can always execute
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool CanExecute(object parameter) => true;

        /// <summary>
        /// Executes the commands Action
        /// </summary>
        /// <param name="parameter"></param>
        public void Execute(object parameter)
        {
            if (typeof(ParameterType) == null || parameter.GetType() == typeof(ParameterType))
                _mAction((ParameterType)parameter);
            else
                throw new ArgumentException("Parameter of type " + parameter.GetType() + ", but it should be " + typeof(ParameterType), "parameter");
        }

        #endregion
    }

    /// <summary>
    /// A basic command that runs an Action
    /// </summary>
    public class RelayCommand : ICommand
    {
        #region Private Members

        /// <summary>
        /// The action to run
        /// </summary>
        private readonly Action mAction;

        #endregion

        #region Public Events

        /// <summary>
        /// The event thats fired when the <see cref="CanExecute(object)"/> value has changed
        /// </summary>
        public event EventHandler CanExecuteChanged = (sender, e) => { };
        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="action">The action that should be performed upon being called</param>
        public RelayCommand(Action action) => mAction = action;

        #endregion

        #region Command Methods

        /// <summary>
        /// A relay command can always execute
        /// </summary>
        /// <param name="parameter">Input parameter (unused)</param>
        /// <returns></returns>
        public bool CanExecute(object parameter) => true;

        /// <summary>
        /// Executes the commands Action
        /// </summary>
        /// <param name="parameter">Input parameter (unused)</param>
        public void Execute(object parameter) => mAction();

        #endregion
    }
}