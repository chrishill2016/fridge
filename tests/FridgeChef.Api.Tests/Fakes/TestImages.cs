namespace FridgeChef.Api.Tests.Fakes;

public static class TestImages
{
    public static byte[] Jpeg(int size = 64) => Pad([0xFF, 0xD8, 0xFF, 0xE0], size);
    public static byte[] Png(int size = 64) => Pad([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], size);
    public static byte[] Webp(int size = 64) => Pad("RIFF\0\0\0\0WEBP"u8.ToArray(), size);
    public static byte[] Gif(int size = 64) => Pad("GIF89a"u8.ToArray(), size);

    private static byte[] Pad(byte[] header, int size)
    {
        var bytes = new byte[Math.Max(size, header.Length)];
        header.CopyTo(bytes, 0);
        return bytes;
    }
}
