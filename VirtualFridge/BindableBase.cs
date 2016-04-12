using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VirtualFridge
{
    /// <summary>
    /// Provides change-notification for classes that derive from it.
    /// Used with XAML data binding.
    /// </summary>
    [System.Runtime.Serialization.DataContract]
    public abstract class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value,
            [CallerMemberName] String propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
