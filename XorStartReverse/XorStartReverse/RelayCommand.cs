using System;
using System.Windows.Input;

namespace XorStartReverse
{
    public class RelayCommand : ICommand
    {
        private Action<object> excecute;
        private Predicate<object> canExcecute;

        public RelayCommand(Action<object> excecute, Predicate<object> canExcecute = null)
        {
            this.excecute = excecute ?? throw new ArgumentNullException(nameof(excecute));
            this.canExcecute = canExcecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return this.canExcecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            this.excecute.Invoke(parameter);
        }
    }
}
