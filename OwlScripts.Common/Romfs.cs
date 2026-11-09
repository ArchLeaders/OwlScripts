using CommunityToolkit.HighPerformance.Buffers;
using OwlScripts.Common.Models;
using Revrs;
using SarcLibrary;

namespace OwlScripts.Common;

public sealed class Romfs(string romfsFolderPath)
{
    private readonly string _romfsFolderPath = romfsFolderPath;

    public void EnumerateLite<TContext>(Action<RomfsFile, TContext> action, Func<RomfsFile, TContext, bool> predicate, TContext context, string fsFilter = "*.*")
    {
        foreach (string file in Directory.EnumerateFiles(_romfsFolderPath, fsFilter, SearchOption.AllDirectories)) {
            var romfsFile = RomfsFile.FromDiskFile(file, _romfsFolderPath);
            if (!predicate(romfsFile, context)) {
                continue;
            }

            using var fs = File.OpenRead(file);
            using var spanOwner = SpanOwner<byte>.Allocate((int)fs.Length);
            fs.ReadExactly(spanOwner.Span);

            // TODO: Check for compression
            // TODO: Check for archive file header/magic (SARC)

            action(romfsFile with { Data = spanOwner.Span }, context);
        }
    }

    private static void EnumerateLiteInArchive<TContext>(in RomfsFile target, Action<RomfsFile, TContext> action, TContext context)
    {
        var reader = new RevrsReader(target.Data);
        var sarc = new ImmutableSarc(ref reader);

        foreach (var (file, data) in sarc) {
            action(new RomfsFile {
                CanonicalFilePath = file,
                FileVersion = target.FileVersion,
                FileAttributes = RomfsFileAttributes.None,
                Data = data
            }, context);
        }
    }
}