namespace DJConnect.Windows;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var page = new MainPage();
        var window = new Window(page)
        {
            Title = "DJConnect"
        };
        window.Destroying += async (_, _) => await page.MarkCleanShutdownAsync();
        return window;
    }
}
