namespace Patcher.Patching
{
    public enum FilePatchStatus
    {
        Unknown,
        ValidFile,
        InvalidFormat,
        InvalidTitle,
        InvalidRegion,
        InvalidVersion,
        InvalidDump,
        GameAlreadyPatched,
        GameWithOldPatch,
    }
}