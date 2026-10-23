using System.Diagnostics.CodeAnalysis;

namespace AppWeb.Core.Validation;

public static class UrlPolicy
{
    public static bool TryNormalize(
        string? value,
        [NotNullWhen(true)] out string? normalized,
        [NotNullWhen(false)] out string? error)
    {
        normalized = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Informe o endereço do site.";
            return false;
        }

        var candidate = value.Trim();

        if (candidate.Any(char.IsControl))
        {
            error = "O endereço contém caracteres inválidos.";
            return false;
        }

        if (!candidate.Contains("://", StringComparison.Ordinal))
        {
            candidate = "https://" + candidate.TrimStart('/');
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            error = "O endereço não é válido.";
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            error = "Use apenas endereços HTTP ou HTTPS.";
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            error = "O endereço não pode conter usuário ou senha.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(uri.Host) || uri.IsUnc || uri.HostNameType == UriHostNameType.Unknown)
        {
            error = "O endereço não possui um host válido.";
            return false;
        }

        normalized = uri.AbsoluteUri;
        return true;
    }

    public static string Normalize(string? value)
    {
        if (!TryNormalize(value, out var normalized, out var error))
        {
            throw new ValidationException(error);
        }

        return normalized;
    }
}
