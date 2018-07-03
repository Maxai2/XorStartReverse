using System;
using System.Windows.Input;

namespace XorStartReverse
{
    public class RelayCommand : ICommand
    {
        private Action<object> excecute;
        private Predicate<object> canExcecute;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public void Execute(object parameter)
        {
            throw new NotImplementedException();
        }
    }
}
