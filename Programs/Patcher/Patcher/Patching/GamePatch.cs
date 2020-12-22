namespace Patcher.Patching
{
    using System.Collections.ObjectModel;

    public record GamePatch(
        Collection<PatchInfo> Patches,
        Collection<InvalidFileInfo> InvalidFiles
    );
}