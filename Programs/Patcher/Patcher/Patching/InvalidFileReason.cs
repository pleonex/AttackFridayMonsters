namespace Patcher.Patching
{
    public enum InvalidFileReason
    {
        InvalidFormat,
        InvalidTitle,
        InvalidRegion,
        InvalidVersion,
        InvalidDump,
        GameAlreadyPatched,
        GameWithOldPatch,
    }
}