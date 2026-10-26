using FluentValidation;

namespace HonestTimeTracker.Application.Tfs;

internal static class TfsCollectionRules
{
    public static string NormalizeUrl(string url) => url.Trim().TrimEnd('/');

    public static IRuleBuilderOptions<T, string> ValidCollectionName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Collection name is required.")
            .MaximumLength(200).WithMessage("Collection name cannot exceed 200 characters.");

    public static IRuleBuilderOptions<T, string> ValidCollectionUrl<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Collection URL is required.")
            .MaximumLength(1000).WithMessage("Collection URL cannot exceed 1000 characters.")
            .Must(BeHttpUrl).WithMessage("Collection URL must be an absolute http(s) address, e.g. https://tfs.company.com/tfs/DefaultCollection.");

    private static bool BeHttpUrl(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
