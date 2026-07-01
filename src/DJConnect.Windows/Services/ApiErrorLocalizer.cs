using DJConnect.Windows.Resources;

namespace DJConnect.Windows.Services;

public static class ApiErrorLocalizer
{
    private const string WindowsAppTypeLabel = "Windows";

    public static string Pairing(string? error, string? message = null)
    {
        return FromPairingCode(error) ?? FromPairingCode(message) ?? AppStrings.Get("ApiError_GenericPairing");
    }

    public static string Pairing(Exception exception)
    {
        if (exception is TaskCanceledException or TimeoutException)
        {
            return AppStrings.Get("ApiError_NetworkTimeout");
        }

        var message = exception.Message;
        if (message.Contains("No such host", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase)
            || message.Contains("nodename nor servname", StringComparison.OrdinalIgnoreCase)
            || message.Contains("host not found", StringComparison.OrdinalIgnoreCase))
        {
            return AppStrings.Get("ApiError_HostNotFound");
        }

        return Pairing(message);
    }

    public static string BackendAction(string? error, string? message = null)
    {
        if (string.Equals(error, "unsupported_backend_capability", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(message)
            && !LooksLikeBackendCode(message))
        {
            return message;
        }

        return FromApiCode(error) ?? FromApiCode(message) ?? AppStrings.Get("ApiError_GenericBackend");
    }

    public static string StaleAuth()
    {
        return AppStrings.Get("ApiError_StaleAuth");
    }

    public static string? FromApiCode(string? codeOrMessage)
    {
        if (string.IsNullOrWhiteSpace(codeOrMessage))
        {
            return null;
        }

        var value = codeOrMessage.Trim();
        var mapped = value.ToLowerInvariant() switch
        {
            "client_type_mismatch" => AppStrings.Format("ApiError_ClientTypeMismatch", WindowsAppTypeLabel),
            "invalid_pair_code" or "wrong_pair_code" or "invalid_code" => AppStrings.Get("ApiError_InvalidPairCode"),
            "invalid_client_type" => AppStrings.Format("ApiError_InvalidClientType", WindowsAppTypeLabel),
            "not_configured" => AppStrings.Get("ApiError_NotConfigured"),
            "unauthorized" or "invalid_token" or "forbidden" => AppStrings.Get("ApiError_Unauthorized"),
            "stale_backend_action" => AppStrings.Get("ApiError_StaleBackendAction"),
            "unsupported_backend_capability" => AppStrings.Get("ApiError_UnsupportedBackendCapability"),
            _ => null
        };

        if (mapped is not null)
        {
            return mapped;
        }

        if (PairingStatePolicy.RequiresLocalPairingCleanup(value))
        {
            return AppStrings.Get("ApiError_StaleAuth");
        }

        return null;
    }

    private static string? FromPairingCode(string? codeOrMessage)
    {
        if (string.IsNullOrWhiteSpace(codeOrMessage))
        {
            return null;
        }

        var value = codeOrMessage.Trim();
        return value.ToLowerInvariant() switch
        {
            "client_type_mismatch" => AppStrings.Format("ApiError_ClientTypeMismatch", WindowsAppTypeLabel),
            "invalid_client_type" => AppStrings.Format("ApiError_InvalidClientType", WindowsAppTypeLabel),
            "invalid_pair_code" or "wrong_pair_code" or "invalid_code" or "not_configured" => AppStrings.Get("ApiError_InvalidPairCode"),
            "unauthorized" or "401" or "403" or "forbidden" => AppStrings.Get("ApiError_InvalidPairCode"),
            "auth_stale" or "stale_auth" => AppStrings.Get("ApiError_StaleAuth"),
            _ => PairingStatePolicy.RequiresLocalPairingCleanup(value)
                ? AppStrings.Get("ApiError_StaleAuth")
                : null
        };
    }

    private static bool LooksLikeBackendCode(string value)
    {
        return value.All(ch => char.IsLower(ch) || char.IsDigit(ch) || ch == '_');
    }
}
