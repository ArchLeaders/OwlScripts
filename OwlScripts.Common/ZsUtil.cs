using Revrs.Extensions;

namespace OwlScripts.Common;

public static class ZsUtil
{
    public const uint ZsMagic = 0xFD2FB528;
    public const uint ZsDictionaryMagic = 0xEC30A437;
    
    public static int GetZsDecompressedSize(this Stream stream)
    {
        Span<byte> header = stackalloc byte[14];
        stream.ReadExactly(header);
        stream.Seek(-14, SeekOrigin.Current);
        return GetDecompressedSize(header);
    }

    public static int GetDecompressedSize(ReadOnlySpan<byte> data)
        => TryGetDecompressedSize(data, out var decompressedSize) ? decompressedSize : data.Length;

    public static bool TryGetDecompressedSize(ReadOnlySpan<byte> data, out int decompressedSize)
    {
        if (data.Read<uint>() != ZsMagic) {
            decompressedSize = -1;
            return false;
        }
        
        var descriptor = data[4];
        var windowDescriptorSize = ((descriptor & 0b00100000) >> 5) ^ 0b1;
        var dictionaryIdFlag = descriptor & 0b00000011;
        var frameContentFlag = descriptor >> 6;

        var offset = dictionaryIdFlag switch {
            0x0 => 5 + windowDescriptorSize,
            0x1 => 5 + windowDescriptorSize + 1,
            0x2 => 5 + windowDescriptorSize + 2,
            0x3 => 5 + windowDescriptorSize + 4,
            _ => throw new OverflowException(
                "Two bits cannot exceed 0x3, something terrible has happened!")
        };

        decompressedSize = frameContentFlag switch {
            0x0 => data[offset],
            0x1 => data[offset..].Read<ushort>() + 0x100,
            0x2 => data[offset..].Read<int>(),
            _ => throw new NotSupportedException(
                "64-bit file sizes are not supported.")
        };
        return true;
    }

    public static int GetDictionaryId(ReadOnlySpan<byte> buffer)
    {
        var descriptor = buffer[4];
        var windowDescriptorSize = ((descriptor & 0b00100000) >> 5) ^ 0b1;
        var dictionaryIdFlag = descriptor & 0b00000011;

        return dictionaryIdFlag switch {
            0x0 => -1,
            0x1 => buffer[5 + windowDescriptorSize],
            0x2 => buffer[(5 + windowDescriptorSize)..].Read<ushort>(),
            0x3 => buffer[(5 + windowDescriptorSize)..].Read<int>(),
            _ => throw new OverflowException(
                "Two bits cannot exceed 0x3, something terrible has happened!")
        };
    }
}