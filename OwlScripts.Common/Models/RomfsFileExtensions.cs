namespace OwlScripts.Common.Models;

[Flags]
public enum RomfsFileAttributes
{
    None = 0,
    HasZsExtension = 1 << 0,
    HasMcExtension = 1 << 1,
    IsProductFile = 1 << 2,
}