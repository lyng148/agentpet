using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using AgentPetCore;

namespace AgentPetApp
{
    public static class SpriteSlicer
    {
        public static ImagePetPack? LoadPack(string directoryPath)
        {
            try
            {
                var manifestPath = Path.Combine(directoryPath, "pet.json");
                if (!File.Exists(manifestPath)) return null;

                var json = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<AgentPetCore.PetManifest>(json);
                if (manifest == null) return null;

                var sheetPath = Path.Combine(directoryPath, manifest.SpritesheetPath);
                if (!File.Exists(sheetPath)) return null;

                var bmpImage = new BitmapImage();
                if (sheetPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                {
                    using var image = SixLabors.ImageSharp.Image.Load(sheetPath);
                    using var ms = new MemoryStream();
                    image.SaveAsPng(ms);
                    ms.Position = 0;
                    bmpImage.BeginInit();
                    bmpImage.StreamSource = ms;
                    bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                    bmpImage.EndInit();
                }
                else
                {
                    bmpImage.BeginInit();
                    bmpImage.UriSource = new Uri(sheetPath, UriKind.Absolute);
                    bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                    bmpImage.EndInit();
                }

                // Ensure pixel format is Bgra32 for easy byte array reading
                var formattedBmp = new FormatConvertedBitmap(bmpImage, PixelFormats.Bgra32, null, 0);

                var clips = Slice(formattedBmp);
                if (clips.Count == 0) return null;

                return new AgentPetCore.ImagePetPack(
                    manifest.Id,
                    manifest.DisplayName,
                    manifest.Description,
                    clips,
                    directoryPath
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading pet pack: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}");
                return null;
            }
        }

        private static List<List<BitmapSource>> Slice(BitmapSource image, byte alphaThreshold = 16)
        {
            int w = image.PixelWidth;
            int h = image.PixelHeight;

            if (w <= 0 || h <= 0) return new List<List<BitmapSource>>();

            int stride = w * 4;
            byte[] pixels = new byte[h * stride];
            image.CopyPixels(pixels, stride, 0);

            bool[] colHas = new bool[w];
            bool[] rowHas = new bool[h];

            for (int y = 0; y < h; y++)
            {
                int rowStart = y * stride;
                bool rowAny = false;
                for (int x = 0; x < w; x++)
                {
                    // Bgra32: B=0, G=1, R=2, A=3
                    if (pixels[rowStart + x * 4 + 3] > alphaThreshold)
                    {
                        colHas[x] = true;
                        rowAny = true;
                    }
                }
                if (rowAny) rowHas[y] = true;
            }

            var colBands = GetSegments(colHas);
            var rowBands = GetSegments(rowHas);

            if (colBands.Count == 0 || rowBands.Count == 0) return new List<List<BitmapSource>>();

            var clips = new List<List<BitmapSource>>();

            foreach (var row in rowBands)
            {
                var clipRow = new List<BitmapSource>();
                foreach (var col in colBands)
                {
                    var rect = new Int32Rect(col.lower, row.lower, col.upper - col.lower, row.upper - row.lower);
                    if (CellHasContent(pixels, stride, rect, alphaThreshold))
                    {
                        var cropped = new CroppedBitmap(image, rect);
                        // Freeze for cross-thread access and performance if needed
                        cropped.Freeze();
                        clipRow.Add(cropped);
                    }
                }
                if (clipRow.Count > 0)
                {
                    clips.Add(clipRow);
                }
            }

            return clips;
        }

        private static List<(int lower, int upper)> GetSegments(bool[] occupancy)
        {
            var result = new List<(int, int)>();
            int? start = null;
            for (int i = 0; i < occupancy.Length; i++)
            {
                if (occupancy[i] && start == null)
                {
                    start = i;
                }
                else if (!occupancy[i] && start != null)
                {
                    result.Add((start.Value, i));
                    start = null;
                }
            }
            if (start != null)
            {
                result.Add((start.Value, occupancy.Length));
            }
            return result;
        }

        private static bool CellHasContent(byte[] pixels, int stride, Int32Rect rect, byte threshold)
        {
            for (int y = rect.Y; y < rect.Y + rect.Height; y++)
            {
                int rowStart = y * stride;
                for (int x = rect.X; x < rect.X + rect.Width; x++)
                {
                    if (pixels[rowStart + x * 4 + 3] > threshold)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
