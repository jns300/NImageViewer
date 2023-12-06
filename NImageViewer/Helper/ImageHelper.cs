using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using SixLabors.ImageSharp.PixelFormats;
using System.Drawing;
using System.Windows.Media.Media3D;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Input;
using SixLabors.ImageSharp.Processing;

namespace NImageViewer.Helper
{
    public class ImageHelper
    {

        /// <summary>
        /// Creates a bitmap image from the memory stream.
        /// </summary>
        /// <param name="ms">the memory stream</param>
        /// <returns>the new bitmap image</returns>
        public static BitmapImage CreateBitmapImage(MemoryStream ms)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        /// <summary>
        /// Obtains default Windows DPI and changes the image when it's DPI is different.
        /// </summary>
        /// <param name="image">the image to modify</param>
        /// <returns>the image with required DPI</returns>
        private static BitmapImage ChangeImageDpi(BitmapImage image)
        {
            float defaultDpi = DeviceCapsHelper.GetDpi();
            if (image is BitmapSource bitmapSource)
            {
                double dpiX = bitmapSource.DpiX;
                double dpiY = bitmapSource.DpiY;
                if ((int)dpiX != defaultDpi || (int)dpiY != defaultDpi)
                {
                    Bitmap result = new Bitmap(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
                    result.SetResolution((float)defaultDpi, (float)defaultDpi);

                    var data = result.LockBits(new System.Drawing.Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    bitmapSource.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
                    result.UnlockBits(data);

                    BitmapImage resultImage = new BitmapImage();
                    using (var stream = new MemoryStream())
                    {
                        result.Save(stream, ImageFormat.Png);
                        stream.Position = 0;
                        resultImage.BeginInit();
                        resultImage.CacheOption = BitmapCacheOption.OnLoad;
                        resultImage.StreamSource = stream;
                        resultImage.EndInit();
                    }
                    return resultImage;
                }
            }
            return image;
        }

        public static BitmapImage GetImageSource(System.Drawing.Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);

                return CreateBitmapImage(ms);
            }

        }
        public static BitmapImage GetImageSourceUsingImageSharp(string path, bool changeDpi)
        {
            return GetImageSourceUsingImageSharp(null, path, changeDpi);
        }
        public static BitmapImage GetImageSourceUsingImageSharp(System.Windows.Media.Color? backgroundColor, string path, bool changeDpi)
        {
            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
            // Image can have written orientation. To take the orientation into account AutoOrient method is called.
            image.Mutate(x => x.AutoOrient());
            if (backgroundColor.HasValue)
            {
                CorrectTransparency(backgroundColor.Value, image);
            }
            using (var ms = new MemoryStream())
            {
                image.SaveAsBmp(ms);
                ms.Seek(0, SeekOrigin.Begin);
                var bitmapImage = CreateBitmapImage(ms);
                if (changeDpi)
                {
                    return ChangeImageDpi(bitmapImage);
                }
                else
                {
                    return bitmapImage;
                }
            }
        }

        /// <summary>
        /// Corrects the transparency by mixing it with the given background color.
        /// </summary>
        /// <param name="backgroundColor">the background color</param>
        /// <param name="image">the modified image</param>
        public static void CorrectTransparency(System.Windows.Media.Color backgroundColor, Image<Rgba32> image)
        {
            image.ProcessPixelRows(accessor =>
            {
                // Color is pixel-agnostic, but it's implicitly convertible to the Rgba32 pixel type
                Rgba32 transparent = SixLabors.ImageSharp.Color.Transparent;

                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                    // pixelRow.Length has the same value as accessor.Width,
                    // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        // Get a reference to the pixel at position x
                        ref Rgba32 pixel = ref pixelRow[x];
                        if (pixel.A < 255)
                        {
                            var newColor = AlphaComposite(backgroundColor, System.Windows.Media.Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B));
                            // Overwrite the pixel referenced by 'ref Rgba32 pixel':
                            pixel = new Rgba32(newColor.R, newColor.G, newColor.B, newColor.A);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Corrects the transparency by mixing it with the given background color.
        /// </summary>
        /// <param name="backgroundColor">the background color</param>
        /// <param name="source">the modified image</param>
        /// <returns>the newly crated image</returns>
        public static BitmapSource CreateTransparency(System.Windows.Media.Color backgroundColor, BitmapSource source)
        {
            if (source.Format != PixelFormats.Bgra32)
            {
                return source;
            }

            var bytesPerPixel = (source.Format.BitsPerPixel + 7) / 8;
            var stride = bytesPerPixel * source.PixelWidth;
            var buffer = new byte[stride * source.PixelHeight];

            source.CopyPixels(buffer, stride, 0);

            for (int y = 0; y < source.PixelHeight; y++)
            {
                for (int x = 0; x < source.PixelWidth; x++)
                {
                    var i = stride * y + bytesPerPixel * x;
                    var b = buffer[i];
                    var g = buffer[i + 1];
                    var r = buffer[i + 2];
                    var a = buffer[i + 3];

                    if (a < 255)
                    {
                        var newColor = AlphaComposite(backgroundColor, System.Windows.Media.Color.FromArgb(a, r, g, b));
                        buffer[i + 0] = newColor.B;
                        buffer[i + 1] = newColor.G;
                        buffer[i + 2] = newColor.R;
                        buffer[i + 3] = newColor.A;
                    }
                }
            }

            return BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
                source.Format, null, buffer, stride);
        }
        public static System.Windows.Media.Color AlphaComposite(System.Windows.Media.Color c1, System.Windows.Media.Color c2)
        {
            var opa1 = c1.A / 255d;
            var opa2 = c2.A / 255d;
            var ar = opa1 + opa2 - (opa1 * opa2);
            var asr = opa2 / ar;
            var a1 = 1 - asr;
            var a2 = asr * (1 - opa1);
            var ab = asr * opa1;
            var r = (byte)(c1.R * a1 + c2.R * a2 + c2.R * ab);
            var g = (byte)(c1.G * a1 + c2.G * a2 + c2.G * ab);
            var b = (byte)(c1.B * a1 + c2.B * a2 + c2.B * ab);
            return System.Windows.Media.Color.FromArgb((byte)(ar * 255), r, g, b);
        }

        /// <summary>
        /// Saves the input image to the given file path.
        /// </summary>
        /// <param name="filePath">the file path</param>
        /// <param name="image">the input image</param>
        public static void SaveToFile(String filePath, BitmapSource image)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
            }
        }

        public static System.Drawing.RectangleF GetScaledRect(System.Drawing.RectangleF rect, float scaleFactor) =>
            new System.Drawing.RectangleF(rect.Location,
            new System.Drawing.SizeF(rect.Width * scaleFactor, rect.Height * scaleFactor));

        /// <summary>
        /// Calculates a new image point after the scaling so that the mouse pointer can point at the same position in the image.
        /// </summary>
        /// <param name="rect">the image rectangle with current location and scaled size</param>
        /// <param name="scaleRatio">the ratio between current scale and the previous scale</param>
        /// <param name="mousePosition">the current mouse pointer location in the image</param>
        /// <returns>the new image location</returns>
        public static System.Windows.Point OffsetScaledRectangleOnMousePosition(System.Windows.Rect rect, double scaleRatio, System.Windows.Point mousePosition)
        {
            var mouseOffset = mousePosition;
            var scaledOffset = new System.Windows.Point(mouseOffset.X * scaleRatio, mouseOffset.Y * scaleRatio);
            var position = new System.Windows.Point(
                rect.X - (scaledOffset.X - mouseOffset.X),
                rect.Y - (scaledOffset.Y - mouseOffset.Y));
            return position;
        }
    }
}
