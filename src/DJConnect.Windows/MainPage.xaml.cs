using DJConnect.Windows.ViewModels;

namespace DJConnect.Windows;

public partial class MainPage : TabbedPage
{
    private readonly MainViewModel _viewModel = new();

    public MainPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }
}
