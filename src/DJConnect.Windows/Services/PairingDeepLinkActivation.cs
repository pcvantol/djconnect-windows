namespace DJConnect.Windows.Services;

public static class PairingDeepLinkActivation
{
    private static readonly object Gate = new();
    private static readonly Queue<string> PendingPayloads = new();

    public static event EventHandler? PayloadQueued;

    public static void Queue(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return;
        }

        lock (Gate)
        {
            PendingPayloads.Enqueue(payload);
        }

        PayloadQueued?.Invoke(null, EventArgs.Empty);
    }

    public static bool TryDequeue(out string payload)
    {
        lock (Gate)
        {
            if (PendingPayloads.Count == 0)
            {
                payload = "";
                return false;
            }

            payload = PendingPayloads.Dequeue();
            return true;
        }
    }
}
