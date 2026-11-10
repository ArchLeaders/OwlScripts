namespace OwlScripts.Common.Extensions;

public static class BgymlExtensions
{
    public static ReadOnlySpan<char> GetBgymlTypeName(this string canonicalFileName)
        => canonicalFileName.AsSpan().GetBgymlTypeName();

    public static ReadOnlySpan<char> GetBgymlTypeName(this ReadOnlySpan<char> canonicalFileName)
    {
        var result = Path.GetExtension(
            Path.GetFileNameWithoutExtension(canonicalFileName)
        );

        return result.IsEmpty ? default : result[1..];
    }
}