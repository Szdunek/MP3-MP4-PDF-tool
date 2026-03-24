using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace ConverterSplitter.Services;

public static class ImageService
{
    public static readonly string[] SupportedExtensions =
        [".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp", ".tiff", ".tif"];

    public static readonly string[] OutputFormats =
        ["PNG", "JPEG", "BMP", "GIF", "WebP", "TIFF"];

    public static async Task ConvertImageAsync(
        string inputPath,
        string outputPath,
        string format,
        int quality = 90,
        int? width = null,
        int? height = null,
        bool keepAspectRatio = true,
        CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync(inputPath, ct);

        if (width.HasValue || height.HasValue)
        {
            var targetWidth = width ?? 0;
            var targetHeight = height ?? 0;

            if (keepAspectRatio)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(targetWidth, targetHeight),
                    Mode = ResizeMode.Max
                }));
            }
            else
            {
                image.Mutate(x => x.Resize(targetWidth, targetHeight));
            }
        }

        IImageEncoder encoder = format.ToUpperInvariant() switch
        {
            "PNG" => new PngEncoder(),
            "JPEG" or "JPG" => new JpegEncoder { Quality = quality },
            "BMP" => new BmpEncoder(),
            "GIF" => new GifEncoder(),
            "WEBP" => new WebpEncoder { Quality = quality },
            "TIFF" => new TiffEncoder(),
            _ => new PngEncoder()
        };

        await image.SaveAsync(outputPath, encoder, ct);
    }

    public static (int width, int height) GetImageDimensions(string path)
    {
        var info = Image.Identify(path);
        return (info.Width, info.Height);
    }

    public static long GetFileSize(string path) => new FileInfo(path).Length;
}
