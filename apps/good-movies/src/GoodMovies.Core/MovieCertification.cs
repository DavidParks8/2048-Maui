namespace GoodMovies.Core;

/// <summary>
/// A normalized, child-safe movie certification.
/// </summary>
public sealed record MovieCertification
{
    public const string GCode = "G";
    public const string PgCode = "PG";

    public MovieCertification(string value)
    {
        if (!TryNormalize(value, out string? normalized))
        {
            throw new ArgumentException(
                "Only G and PG certifications are supported.",
                nameof(value)
            );
        }

        Code = normalized!;
    }

    public string Code { get; }

    public string Value => Code;

    public bool IsG => Code == GCode;

    public bool IsPg => Code == PgCode;

    public static MovieCertification G { get; } = new(GCode);

    public static MovieCertification PG { get; } = new(PgCode);

    public static bool TryCreate(string? value, out MovieCertification certification)
    {
        if (TryNormalize(value, out string? normalized))
        {
            certification = normalized == GCode ? G : PG;
            return true;
        }

        certification = null!;
        return false;
    }

    public static bool TryParse(string? value, out MovieCertification certification) =>
        TryCreate(value, out certification);

    public static MovieCertification Parse(string value) => new(value);

    public static bool IsAllowed(string? value) => TryCreate(value, out _);

    public static bool IsAllowed(MovieCertification? certification) => certification is not null;

    public static implicit operator string?(MovieCertification? certification) =>
        certification?.Code;

    public override string ToString() => Code;

    private static bool TryNormalize(string? value, out string? normalized)
    {
        normalized = value?.Trim().ToUpperInvariant();
        return normalized is GCode or PgCode;
    }
}
