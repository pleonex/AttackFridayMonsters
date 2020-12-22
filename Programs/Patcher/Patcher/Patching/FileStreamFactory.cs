using System.IO;

namespace Patcher.Patching
{
    public static class FileStreamFactory
    {
        public static FileStream OpenForRead(string filePath)
        {
            return new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,   // Default buffer, it seems bigger does not affect time
                FileOptions.RandomAccess);
        }

        public static FileStream CreateForWriteAndRead(string filePath)
        {
            return new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.Read,
                4096,   // Default buffer, it seems bigger does not affect time
                FileOptions.RandomAccess);
        }
    }
}
