using System.Diagnostics.CodeAnalysis;

namespace GoodMovies.Core;

/// <summary>
/// A normalized, child-safe movie certification.
/// </summary>
public sealed record MovieCertification
{
    private const string GCode = "G";
    private const string PgCode = "PG";

    private static readonly MovieCertification G = new(GCode);
    private static readonly MovieCertification PG = new(PgCode);

    private MovieCertification(string code)
    {
        Code = code;
    }

    public string Code { get; }

    public bool IsG => Code == GCode;

    public bool IsPg => Code == PgCode;

    public static bool TryCreate(
        string? value,
        [NotNullWhen(true)] out MovieCertification? certification
    )
    {
        string? normalized = value?.Trim();
        if (string.Equals(normalized, GCode, StringComparison.OrdinalIgnoreCase))
        {
            certification = G;
            return true;
        }

        if (string.Equals(normalized, PgCode, StringComparison.OrdinalIgnoreCase))
        {
            certification = PG;
            return true;
        }

        certification = null;
        return false;
    }

    public override string ToString() => Code;
}
