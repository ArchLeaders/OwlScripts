using OwlScripts.Common.Extensions;

namespace OwlScripts.Common.Models;

public readonly ref struct RomfsFile
{
    public required string CanonicalFilePath { get; init; }

    public required int FileVersion { get; init; }

    public required RomfsFileAttributes FileAttributes { get; init; }

    public string? FilePath { get; init; }

    public Span<byte> Data { get; init; }

    public static RomfsFile FromDiskFile(string filePath, ReadOnlySpan<char> romfsRoot)
    {
        return new RomfsFile {
            CanonicalFilePath = filePath.GetCanonical(romfsRoot, out int fileVersion, out var attributes).ToString(),
            FileVersion = fileVersion,
            FileAttributes = attributes,
            FilePath = filePath
        };
    }
}