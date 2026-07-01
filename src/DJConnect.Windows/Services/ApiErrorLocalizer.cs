using DJConnect.Windows.Resources;

namespace DJConnect.Windows.Services;

public static class ApiErrorLocalizer
{
    public static string Pairing(string? error, string? message = null)
    {
        return FromApiCode(error) ?? FromApiCode(message) ?? AppStrings.Get("ApiError_GenericPairing");
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
            "client_type_mismatch" => AppStrings.Get("ApiError_ClientTypeMismatch"),
            "invalid_pair_code" or "wrong_pair_code" or "invalid_code" => AppStrings.Get("ApiError_InvalidPairCode"),
            "invalid_client_type" => AppStrings.Get("ApiError_InvalidClientType"),
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

    private static bool LooksLikeBackendCode(string value)
    {
        return value.All(ch => char.IsLower(ch) || char.IsDigit(ch) || ch == '_');
    }
}
