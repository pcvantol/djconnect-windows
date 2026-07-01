using DJConnect.Windows.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace DJConnect.Windows.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
        var instance = AppInstance.GetCurrent();
        QueueActivationPayload(instance.GetActivatedEventArgs());
        instance.Activated += (_, args) => QueueActivationPayload(args);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    private static void QueueActivationPayload(AppActivationArguments args)
    {
        if (args.Kind == ExtendedActivationKind.Protocol
            && args.Data is IProtocolActivatedEventArgs protocolArgs)
        {
            PairingDeepLinkActivation.Queue(protocolArgs.Uri?.ToString());
        }
    }
}
