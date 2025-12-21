using System.ComponentModel;

namespace BattleShips.Core
{
    public interface INotifyPropertyChanged
    {
        event PropertyChangedEventHandler PropertyChanged;
    }

    public class PropertyChangedEventArgs : EventArgs
    {
        public string PropertyName { get; }

        public PropertyChangedEventArgs(string propertyName)
        {
            PropertyName = propertyName;
        }
    }

    public delegate void PropertyChangedEventHandler(object sender, PropertyChangedEventArgs e);
}