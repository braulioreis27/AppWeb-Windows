using AppWeb.Core.Models;

namespace AppWeb.Core.Validation;

public static class WebAppValidator
{
    private const int MaxNameLength = 80;
    private const int MaxDescriptionLength = 500;

    public static WebAppDraft Normalize(WebAppDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var name = (draft.Name ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ValidationException("Informe um nome para o Web App.");
        }

        if (name.Length > MaxNameLength)
        {
            throw new ValidationException($"O nome deve ter no máximo {MaxNameLength} caracteres.");
        }

        var description = string.IsNullOrWhiteSpace(draft.Description) ? null : draft.Description.Trim();
        if (description is { Length: > MaxDescriptionLength })
        {
            throw new ValidationException($"A descrição deve ter no máximo {MaxDescriptionLength} caracteres.");
        }

        var browserId = (draft.BrowserId ?? string.Empty).Trim();
        if (browserId.Length == 0)
        {
            throw new ValidationException("Selecione um navegador.");
        }

        return draft with
        {
            Name = name,
            Description = description,
            Url = UrlPolicy.Normalize(draft.Url),
            BrowserId = browserId
        };
    }
}
