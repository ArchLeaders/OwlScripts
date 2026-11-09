using System.Runtime.CompilerServices;
using OwlScripts.Common.Models;

namespace OwlScripts.Common.Extensions;

public static class RomfsFileExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetCanonical(this string fileRelativeToRomfs)
    {
        return fileRelativeToRomfs.AsSpan().GetCanonical([], out _, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetCanonical(this ReadOnlySpan<char> fileRelativeToRomfs)
    {
        return fileRelativeToRomfs.GetCanonical([], out _, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetCanonical(this string fileRelativeToRomfs, out int fileVersion, out RomfsFileAttributes attributes)
    {
        return fileRelativeToRomfs.GetCanonical([], out fileVersion, out attributes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetCanonical(this ReadOnlySpan<char> fileRelativeToRomfs, out int fileVersion, out RomfsFileAttributes attributes)
    {
        return fileRelativeToRomfs.GetCanonical([], out fileVersion, out attributes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetCanonical(this string file, ReadOnlySpan<char> romfs)
    {
        return file.AsSpan().GetCanonical(romfs, out _, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetCanonical(this string file, ReadOnlySpan<char> romfs, out int fileVersion, out RomfsFileAttributes attributes)
    {
        return file.AsSpan().GetCanonical(romfs, out fileVersion, out attributes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetCanonical(this ReadOnlySpan<char> file, ReadOnlySpan<char> romfs)
    {
        return file.GetCanonical(romfs, out _, out _);
    }

    public static unsafe ReadOnlySpan<char> GetCanonical(this ReadOnlySpan<char> file, ReadOnlySpan<char> romfs, out int fileVersion, out RomfsFileAttributes attributes)
    {
        if (file.Length < romfs.Length) {
            throw new ArgumentException(
                $"The provided {nameof(romfs)} path is longer than the input {nameof(file)}.", nameof(romfs)
            );
        }

        fileVersion = -1;
        attributes = RomfsFileAttributes.None;

        var size = file.Length - romfs.Length - file[^3..] switch {
            ".zs" => (int)(attributes |= RomfsFileAttributes.HasZsExtension) + 2,
            ".mc" => (int)(attributes |= RomfsFileAttributes.HasMcExtension) + 1,
            _ => 0
        };

        // Make a copy to avoid
        // mutating the input string
        var result = file[romfs.Length..(romfs.Length + size)].ToString();

        Span<char> canonical;

        fixed (char* ptr = result) {
            canonical = new Span<char>(ptr, size);
        }

        var state = State.Default;
        for (var i = 0; i < size; i++) {
            ref var @char = ref canonical[i];

            if (@char is '.' && !canonical[^7..].Equals(".pchtxt", StringComparison.OrdinalIgnoreCase)) {
                switch (size - i) {
                    case > 2 when canonical[i..(i + 2)] is ".1" && (canonical.Length < 5 || canonical[^5..] is not ".txtg"):
                        attributes |= RomfsFileAttributes.IsProductFile;
                        size -= 4;
                        if (int.TryParse(canonical[(i + 1)..(i + 4)], out var version)) {
                            fileVersion = version;
                            state = State.SkipVersion;
                        }

                        break;
                    case > 8 when canonical[i..(i + 8)] is ".Product":
                        attributes |= RomfsFileAttributes.IsProductFile;
                        size -= 4;
                        i += 8;
                        fileVersion = int.Parse(canonical[(i + 1)..(i + 4)]);
                        state = State.SkipVersion;
                        break;
                }
            }

            @char = state switch {
                State.Default => @char,
                _ => @char = canonical[i + 4]
            };

            @char = @char switch {
                '\\' => '/',
                _ => @char
            };
        }

        return canonical[0] switch {
            '/' => canonical[1..size],
            _ => canonical[..size]
        };
    }
}

file enum State
{
    Default = 0,
    SkipVersion = 1
}