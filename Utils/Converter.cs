namespace ImageHost.Utils
{
    public static class Converter
    {
        public static string ByteToReadableString(double byteSize)
        {
            string[] sizes = {"B", "KB", "MB", "GB", "TB"};
            ;
            int order = 0;
            while (byteSize >= 1024 && order < sizes.Length - 1)
            {
                order++;
                byteSize = byteSize / 1024;
            }

            return string.Format("{0:0.##} {1}", byteSize, sizes[order]);
        }
    }
}