using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace AuctionSystem.Services;

/// <summary>
/// Загружает изображения аукционов и лотов из папки Images рядом с exe.
/// В базе хранится только имя файла; папка выбирается по типу объекта.
/// </summary>
public static class PhotoService
{
    private static readonly string ImagesRoot =
        Path.Combine(AppContext.BaseDirectory, "Images");

    private static readonly string AuctionsRoot = Path.Combine(ImagesRoot, "Auctions");
    private static readonly string LotsRoot = Path.Combine(ImagesRoot, "Lots");
    private static readonly string PlaceholderPath = Path.Combine(ImagesRoot, "placeholder.jpg");

    public static BitmapImage? LoadAuction(string? fileName) => Load(AuctionsRoot, fileName);

    public static BitmapImage? LoadLot(string? fileName) => Load(LotsRoot, fileName);

    public static string ImagesDirectory => ImagesRoot;

    private static BitmapImage? Load(string folder, string? fileName)
    {
        var path = Resolve(folder, fileName);
        if (path is not null)
        {
            var image = TryCreate(path);
            if (image is not null)
            {
                return image;
            }
        }

        return TryCreate(PlaceholderPath);
    }

    private static string? Resolve(string folder, string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        // В базе хранится только имя файла. GetFileName не даёт выйти за пределы папки.
        var safeName = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(safeName))
        {
            return null;
        }

        var path = Path.Combine(folder, safeName);
        return File.Exists(path) ? path : null;
    }

    private static BitmapImage? TryCreate(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var image = new BitmapImage();
            using (var stream = File.OpenRead(path))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
            }

            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
