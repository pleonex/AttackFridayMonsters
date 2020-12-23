namespace Patcher.Patching
{
    public enum FilePatchStatus
    {
        Unknown,
        NoFile,
        ValidFile,
        InvalidFormat,
        InvalidTitle,
        InvalidRegion,
        InvalidVersion,
        GameIsEncrypted,
        InvalidDump,
    }
}