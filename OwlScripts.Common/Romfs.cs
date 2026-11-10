using CommunityToolkit.HighPerformance.Buffers;
using OwlScripts.Common.Models;
using Revrs;
using Revrs.Extensions;
using SarcLibrary;
using ZstdSharp;

namespace OwlScripts.Common;

public sealed class Romfs
{
    private const int ZsDefaultCompressionLevel = 7;
    public const uint SarcMagic = 0x43524153;

    private readonly string _romfsFolderPath;
    private readonly Decompressor _zsDefaultDecompressor = new();
    private readonly Dictionary<int, Decompressor> _zsDecompressors = [];
    private readonly Compressor _zsDefaultCompressor = new(ZsDefaultCompressionLevel);
    private readonly Dictionary<int, Compressor> _zsCompressors = [];

    /// <summary>
    /// The ZStandard compression level to use when recompressing files
    /// </summary>
    public int CompressionLevel {
        get => _zsDefaultCompressor.Level;
        set {
            _zsDefaultCompressor.Level = value;
            foreach (var (_, compressor) in _zsCompressors) {
                compressor.Level = value;
            }
        }
    }

    public Romfs(string romfsFolderPath)
    {
        _romfsFolderPath = romfsFolderPath;

        LoadZsDictionaries();
    }
    
    public void EnumerateLite<TContext>(Action<RomfsFile, TContext> action, Func<RomfsFile, TContext, bool> predicate, TContext context, params ReadOnlySpan<string> fsFilters)
    {
        foreach (string fsFilter in fsFilters) {
            EnumerateLite(action, predicate, context, fsFilter);
        }
    }

    public void EnumerateLite<TContext>(Action<RomfsFile, TContext> action, Func<RomfsFile, TContext, bool> predicate, TContext context, string fsFilter = "*.*")
    {
        foreach (string file in Directory.EnumerateFiles(_romfsFolderPath, fsFilter, SearchOption.AllDirectories)) {
            var romfsFile = RomfsFile.FromDiskFile(file, _romfsFolderPath);
            if (!predicate(romfsFile, context)) {
                continue;
            }

            using var fs = File.OpenRead(file);
            using var spanOwner = GetDecompressed(fs);

            romfsFile = romfsFile with { Data = spanOwner.Span };

            if (spanOwner.Span.Read<uint>() == SarcMagic) {
                EnumerateLiteInArchive(romfsFile, action, predicate, context);
                continue;
            }

            action(romfsFile, context);
        }
    }

    private static void EnumerateLiteInArchive<TContext>(in RomfsFile target, Action<RomfsFile, TContext> action, Func<RomfsFile, TContext, bool> predicate, TContext context)
    {
        var reader = new RevrsReader(target.Data);
        var sarc = new ImmutableSarc(ref reader);

        foreach (var (file, data) in sarc) {
            var romfsFile = new RomfsFile {
                CanonicalFilePath = file,
                FileVersion = target.FileVersion,
                FileAttributes = RomfsFileAttributes.None,
                Data = data
            };
            
            if (!predicate(romfsFile, context)) {
                continue;
            }
            
            action(romfsFile, context);
        }
    }

    public SpanOwner<byte> GetDecompressed(FileStream fs)
    {
        var spanOwner = SpanOwner<byte>.Allocate((int)fs.Length);
        var raw = spanOwner.Span;
        fs.ReadExactly(raw);

        if (!ZsUtil.TryGetDecompressedSize(raw, out int decompressedSize)) {
            return spanOwner;
        }

        var spanOwnerDecompressed = SpanOwner<byte>.Allocate(decompressedSize);

        try {
            var decompressed = spanOwnerDecompressed.Span;
            int decompressorId = ZsUtil.GetDictionaryId(raw); 
            var decompressor = _zsDecompressors.GetValueOrDefault(decompressorId, _zsDefaultDecompressor);
            if (!decompressor.TryUnwrap(raw, decompressed, out int written)) {
                throw new InvalidOperationException(
                    $"Attempt to decompress {fs.Name} with {{ ZsDecompressor ID={decompressorId} }} failed");
            }

            if (written != decompressed.Length) {
                throw new InvalidDataException(
                    $"Decompressed size was expected to be {decompressed.Length} bytes, " +
                    $"but {written} bytes were decompressed");
            }

            return spanOwnerDecompressed;
        }
        catch {
            spanOwnerDecompressed.Dispose();
            throw;
        }
        finally {
            spanOwner.Dispose();
        }
    }

    private void LoadZsDictionaries()
    {
        var zsPackFilePath = Path.Combine(_romfsFolderPath, "Pack", "ZsDic.pack.zs");
        using var fs = File.OpenRead(zsPackFilePath);
        using var spanOwner = GetDecompressed(fs);

        RevrsReader reader = new(spanOwner.Span);
        ImmutableSarc sarc = new(ref reader);
        foreach (var (_, fileData) in sarc) {
            if (fileData.Length < 8 || fileData.Read<uint>() != ZsUtil.ZsDictionaryMagic) {
                continue;
            }

            Decompressor decompressor = new();
            decompressor.LoadDictionary(fileData);
            _zsDecompressors[fileData[4..].Read<int>()] = decompressor;

            Compressor compressor = new(CompressionLevel);
            compressor.LoadDictionary(fileData);
            _zsCompressors[fileData[4..].Read<int>()] = compressor;
        }
    }
}