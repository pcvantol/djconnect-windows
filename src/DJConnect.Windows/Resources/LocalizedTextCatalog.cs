using System.ComponentModel;

namespace DJConnect.Windows.Resources;

public sealed class LocalizedTextCatalog : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => AppStrings.Get(key);

    public string Get(string key) => AppStrings.Get(key);

    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
