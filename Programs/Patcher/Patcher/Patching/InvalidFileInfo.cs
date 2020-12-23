namespace Patcher.Patching
{
    public record InvalidFileInfo(
        string TitleId,
        string Hash,
        FilePatchStatus Reason);
}