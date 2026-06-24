namespace DJConnect.Windows.Services;

public static class MonkeyTestMode
{
    private static readonly string[] EnvironmentNames =
    [
        "DJCONNECT_DEMO_MONKEY_TEST",
        "DJCONNECT_MONKEY_TEST",
        "DJCONNECT_UI_TEST",
        "MONKEY_TEST",
        "UITEST"
    ];

    public static bool IsEnabled => EnvironmentNames.Any(IsTruthyEnvironment);

    public static bool IsTruthyEnvironment(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
    }
}
