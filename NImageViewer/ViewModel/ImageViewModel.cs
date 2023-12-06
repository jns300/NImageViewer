using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NImageViewer.Helper;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace NImageViewer.ViewModel
{
    internal class ImageViewModel : INotifyPropertyChanged
    {
        private const string AppTitle = "NImageViewer";

        private readonly string[] validExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".gif", ".webp" };

        private readonly SortedSet<string> extensionSet;

        private string windowTitle = AppTitle;

        private string statusText = String.Empty;

        private string? startImagePath;

        private BitmapImage? nonScaledImage;

        private BitmapImage? bitmapImage;

        private BitmapSource? imageSource;

        private ScrollBarVisibility scrollVisibility = ScrollBarVisibility.Disabled;

        private System.Windows.Media.Color backgroundColor = System.Windows.Media.Color.FromRgb(68, 68, 68);

        private double currentScale;

        private System.Windows.Size viewportSize;

        public event EventHandler? ImageSet;

        public event PropertyChangedEventHandler? PropertyChanged;
        public ImageViewModel()
        {
            extensionSet = new SortedSet<string>(validExtensions, StringComparer.OrdinalIgnoreCase);
            UpdateWindowTitle();
        }

        public System.Windows.Media.Brush BackgroundColor => new SolidColorBrush(backgroundColor);

        public void OpenImage(String newPath)
        {
            bitmapImage = null;
            StatusText = String.Empty;
            currentScale = -1d;
            startImagePath = newPath;
            ScrollVisibility = ScrollBarVisibility.Disabled;
            nonScaledImage = GetImageSource(newPath);
            ImageSource = nonScaledImage;
            ImageSet?.Invoke(this, EventArgs.Empty);
            UpdateWindowTitle();
            UpdateStatusText();
        }

        private BitmapImage? GetImageSource(string? path)
        {
            try
            {
                if (path == null) return null;
                if (!File.Exists(path))
                {
                    throw new InvalidOperationException($"Image path '{path}' does not exist.");
                }
                // Using System.Drawing.Image provides sometimes odd results, like black background when it is white.
                // Examples for this incorrectness are in 'My/Pictures' (in polish) folder.
                return ImageHelper.GetImageSourceUsingImageSharp(backgroundColor, path, false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read image from path '{path}', reason: {ex.Message}.");
            }
        }

        public void ChangeScrollVisibility()
        {
            if (scrollVisibility == ScrollBarVisibility.Disabled)
            {
                ScrollVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                ScrollVisibility = ScrollBarVisibility.Disabled;
                ScaleImage(-1d);
            }
        }
        public String? ImagePath => startImagePath;
        public ScrollBarVisibility ScrollVisibility
        {
            get => scrollVisibility;
            set
            {
                SetProperty(ref scrollVisibility, value);
            }
        }


        public BitmapSource? ImageSource
        {
            get => imageSource;
            set
            {
                SetProperty(ref imageSource, value);
            }
        }

        public string WindowTitle
        {
            get => windowTitle;
            set
            {
                SetProperty(ref windowTitle, value);
            }
        }

        public string StatusText
        {
            get => statusText;
            set
            {
                SetProperty(ref statusText, value);
            }
        }

        public BitmapImage? CurrentBitmapImage
        {
            get
            {
                if (bitmapImage != null) return bitmapImage;
                if (startImagePath != null)
                {
                    bitmapImage = ImageHelper.GetImageSourceUsingImageSharp(startImagePath, false);
                }
                return bitmapImage;
            }
        }

        public double ImageWidth => imageSource?.Width ?? 0d;

        public double ImageHeight => imageSource?.Height ?? 0d;

        public bool HasImage => imageSource != null;

        public void UpdateStatusText()
        {
            if (nonScaledImage != null)
            {
                StatusText = $"{ScaleText}\t{nonScaledImage.PixelWidth} x {nonScaledImage.PixelHeight}\tDpiX: {nonScaledImage.DpiX:0.0} DpiY: {nonScaledImage.DpiY:0.0}";
            }
            else
            {
                StatusText = String.Empty;
            }
        }
        public String ScaleText
        {
            get
            {
                if (ImageSource == null) return String.Empty;
                double scaleToUse = GetCurrentScale();

                return String.Format("{0:0.0%}", scaleToUse);
            }
        }
        public void UpdateWindowTitle()
        {
            if (String.IsNullOrWhiteSpace(ImagePath))
            {
                WindowTitle = AppTitle;
            }
            else
            {
                WindowTitle = $"{ImagePath} - {AppTitle}";
            }
        }

        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "",  //$NON-NLS-1$
            Action<T, T>? onChanged = null)
        {
            return SetPropertyValue(ref backingStore, value, propertyName, onChanged, this, PropertyChanged);
        }
        public static bool SetPropertyValue<T>(ref T backingStore, T value,
           string propertyName, Action<T, T>? onChanged, object sender, PropertyChangedEventHandler? changedEventHandler)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;
            T oldValue = backingStore;
            backingStore = value;
            onChanged?.Invoke(oldValue, value);
            InvokeChangeEventHandler(sender, propertyName, changedEventHandler);
            return true;
        }
        public static void InvokeChangeEventHandler(object sender, string propertyName, PropertyChangedEventHandler? changedEventHandler)
        {
            changedEventHandler?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
        }
        internal void MovePrevious()
        {
            MoveToItem(-1);
        }

        internal void MoveNext()
        {
            MoveToItem(1);
        }

        private void MoveToItem(int offset)
        {
            if (startImagePath != null)
            {
                String fullStart = Path.GetFullPath(startImagePath);
                string? dir = Path.GetDirectoryName(fullStart);
                if (dir != null && Directory.Exists(dir))
                {
                    string[] fileList = Directory.GetFiles(dir, "*.*") //$NON-NLS-1$
                        .Where(f => extensionSet.Contains(Path.GetExtension(f))).ToArray();
                    Array.Sort(fileList, StringComparer.OrdinalIgnoreCase);
                    int index = Array.IndexOf(fileList, fullStart);
                    string? newPath = null;
                    if (index == -1)
                    {
                        if (fileList.Length > 0)
                        {
                            newPath = fileList[0];
                        }
                    }
                    else
                    {
                        int newIndex = (index + offset) % fileList.Length;
                        if (newIndex < 0)
                        {
                            newIndex = fileList.Length - 1;
                        }
                        newPath = fileList[newIndex];
                    }
                    if (newPath != null)
                    {
                        OpenImage(newPath);
                    }
                }
            }
        }

        public void SetImageControlSize(System.Windows.Size viewportSize)
        {
            this.viewportSize = viewportSize;
            UpdateStatusText();
        }

        internal double GetCurrentScale()
        {
            if (nonScaledImage != null)
            {
                return Math.Max(viewportSize.Width / nonScaledImage.Width, viewportSize.Height / nonScaledImage.Height);
            }
            return -1d;
        }

        internal double RecentSetCurrentScale => currentScale;

        internal void ScaleImage(double scale)
        {
            if (imageSource != null)
            {
                currentScale = scale;
                if (scale < 0d)
                {
                    ImageSource = nonScaledImage;
                }
                else
                {
                    var targetBitmap = new TransformedBitmap(nonScaledImage, new ScaleTransform(scale, scale));
                    ImageSource = targetBitmap;
                }
                UpdateStatusText();
            }
        }
    }
}
