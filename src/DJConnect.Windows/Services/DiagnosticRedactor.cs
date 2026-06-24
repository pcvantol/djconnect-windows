using System.Text.RegularExpressions;

namespace DJConnect.Windows.Services;

public static class DiagnosticRedactor
{
    public static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        var redacted = text;
        var options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        redacted = Regex.Replace(redacted, @"Authorization\s*:\s*Bearer\s+[A-Za-z0-9._~+/=-]+", "Authorization: <redacted>", options);
        redacted = Regex.Replace(redacted, @"Bearer\s+[A-Za-z0-9._~+/=-]{16,}", "Bearer <redacted-token>", options);
        redacted = Regex.Replace(redacted, @"\b(djconnect[-_])?bearer[-_\s]?token\b\s*[:=]\s*[""']?[^""'\s,;]+", "$1bearer_token: <redacted-token>", options);
        redacted = Regex.Replace(redacted, @"\b(device[-_\s]?token|access[-_\s]?token|refresh[-_\s]?token|id[-_\s]?token)\b\s*[:=]\s*[""']?[^""'\s,;]+", "$1: <redacted-token>", options);
        redacted = Regex.Replace(redacted, @"\b(pairing_code|pair_code|pairing[-_\s]?token)\b\s*[:=]\s*[""']?[^""'\s,;]+", "$1: <redacted-pairing-code>", options);
        redacted = Regex.Replace(redacted, @"\bbootstrap_proof\b\s*[:=]\s*[""']?[^""'\s,;]+", "bootstrap_proof: <redacted-bootstrap-proof>", options);
        redacted = Regex.Replace(redacted, @"\beyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{10,}\b", "<redacted-ha-token>", options);
        redacted = Regex.Replace(redacted, @"\b(push[-_\s]?token)\b\s*[:=]\s*[""']?[^""'\s,;]+", "$1: <redacted-push-token>", options);
        redacted = Regex.Replace(redacted, @"\b(cookie|secret|password|passwd|api[-_\s]?key|apikey|client[-_\s]?secret)\b\s*[:=]\s*[""']?[^""'\r\n,;]+", "$1: <redacted-secret>", options);
        redacted = Regex.Replace(redacted, @"https?://(?:localhost|127\.0\.0\.1|10\.\d{1,3}\.\d{1,3}\.\d{1,3}|172\.(?:1[6-9]|2\d|3[0-1])\.\d{1,3}\.\d{1,3}|192\.168\.\d{1,3}\.\d{1,3}|[^/\s]*\.local)(?::\d+)?[^\s)]*", "<redacted-url>", options);
        redacted = Regex.Replace(redacted, @"\b\d{6}\b", "<redacted-pairing-code>", options);
        return redacted;
    }
}
