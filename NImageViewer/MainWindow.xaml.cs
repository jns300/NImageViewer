using Microsoft.Win32;
using NImageViewer.Controls;
using NImageViewer.Helper;
using NImageViewer.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace NImageViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double validSize;

        private double validScale;

        private ImageViewModel viewModel;

        private ScrollDragger dragger;

        private const double ScrollOffset = 12;

        private bool isFullScreen;

        private WindowState normalState;

        private WindowStyle normalStyle;

        private String? activationStartPath;

        public MainWindow()
        {
            InitializeComponent();
            var args = Environment.GetCommandLineArgs();
            string? startPath = null;
            if (args?.Length >= 2 && File.Exists(args[1]))
            {
                startPath = args[1];
            }
            DataContext = viewModel = new ImageViewModel();
            viewModel.ImageSet += ViewModel_ImageSet;
            dragger = new ScrollDragger(image, scrollView);
            HandleImageSet();
            activationStartPath = startPath;

        }
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            if (activationStartPath != null)
            {
                viewModel.OpenImage(activationStartPath);
                activationStartPath = null;
            }
        }
        private void ViewModel_ImageSet(object? sender, EventArgs e)
        {
            try
            {
                HandleImageSet();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Image display error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleImageSet()
        {
            image.Stretch = Stretch.None;
            TryStretchImage();
        }

        private void TryStretchImage()
        {
            if (viewModel?.ImageSource != null)
            {
                if (viewModel.ImageSource.Width > scrollView.ViewportWidth || viewModel.ImageSource.Height > scrollView.ViewportHeight)
                {
                    image.Stretch = Stretch.Uniform;
                }
            }

        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    viewModel.ChangeScrollVisibility();
                    dragger.IsEnabled = viewModel.ScrollVisibility != ScrollBarVisibility.Disabled;
                    if (!dragger.IsEnabled)
                    {
                        TryStretchImage();
                    }
                }
                else if (e.Key == Key.Left)
                {
                    if (scrollView.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
                    {
                        scrollView.ScrollToHorizontalOffset(scrollView.HorizontalOffset - ScrollOffset);
                    }
                    else
                    {
                        viewModel.MovePrevious();
                    }
                }
                else if (e.Key == Key.Right)
                {
                    if (scrollView.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
                    {
                        scrollView.ScrollToHorizontalOffset(scrollView.HorizontalOffset + ScrollOffset);
                    }
                    else
                    {
                        viewModel.MoveNext();
                    }
                }
                else if (e.Key == Key.Up)
                {
                    if (scrollView.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                    {
                        scrollView.ScrollToVerticalOffset(scrollView.VerticalOffset - ScrollOffset);
                    }
                }
                else if (e.Key == Key.Down)
                {
                    if (scrollView.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                    {
                        scrollView.ScrollToVerticalOffset(scrollView.VerticalOffset + ScrollOffset);
                    }
                }
                else if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        FileName = "Select an image file",
                        Filter = "Image files (*.png)|*.png|(*.bmp)|*.bmp|(*.jpg)|*.jpg|(*.gif)|*.gif|(*.webp)|*.webp|(*.tiff)|*.tiff|(*.*)|*.*",
                        Title = "Open image file"
                    };
                    if (openFileDialog.ShowDialog() == true)
                    {
                        viewModel.OpenImage(openFileDialog.FileName);
                    }
                }
                else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    CopyToClipboard();
                }
                else if (e.Key == Key.F11)
                {
                    FullScreenRequest();
                }
                else if (e.Key == Key.Escape)
                {
                    ExitFullScreen();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Image display error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyToClipboard()
        {
            if (viewModel.CurrentBitmapImage == null)
            {
                return;
            }
            Clipboard.SetImage(viewModel.CurrentBitmapImage);
        }

        private void FullScreenRequest()
        {
            if (!isFullScreen)
            {
                isFullScreen = true;
                normalState = WindowState;
                normalStyle = WindowStyle;
                WindowStyle = WindowStyle.None;
                // First enter the normal state then maximized. In other case task bar will be visible.
                WindowState = WindowState.Normal;
                WindowState = WindowState.Maximized;
                ResizeMode = ResizeMode.NoResize;
                lblStatus.Visibility = Visibility.Collapsed;
            }
            else
            {
                ExitFullScreen();
            }
        }

        private void ExitFullScreen()
        {
            if (isFullScreen)
            {
                isFullScreen = false;
                WindowStyle = normalStyle;
                WindowState = normalState;
                ResizeMode = ResizeMode.CanResize;
                lblStatus.Visibility = Visibility.Visible;
            }
        }

        public static bool WasOverflow { get; set; }
        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (viewModel.ImageSource == null) return;
                Point position = e.GetPosition(image);
                double scale = viewModel.GetCurrentScale();
                double previousScale = scale;
                scale *= 1 + Math.Sign(e.Delta) * 0.12;
                if (viewModel.HasImage)
                {
                    if (!WasOverflow)
                    {
                        validSize = viewModel.ImageWidth * viewModel.ImageHeight;
                        validScale = previousScale;
                    }
                    else
                    {
                        WasOverflow = false;
                        scale = validScale;
                    }
                }
                double newWidth = viewModel.ImageSource.Width * scale;
                double newHeight = viewModel.ImageSource.Height * scale;
                if (scale > 1e-3)
                {
                    viewModel.ScaleImage(scale);
                    viewModel.ScrollVisibility = ScrollBarVisibility.Auto;
                    image.Stretch = Stretch.None;
                    dragger.IsEnabled = true;
                    var scaledRect = new Rect(-scrollView.HorizontalOffset, -scrollView.VerticalOffset, viewModel.ImageWidth, viewModel.ImageHeight);
                    double scaleRatio = scale / previousScale;
                    var newRect = ImageHelper.OffsetScaledRectangleOnMousePosition(scaledRect, scaleRatio, position);
                    scrollView.ScrollToHorizontalOffset(-newRect.X);
                    scrollView.ScrollToVerticalOffset(-newRect.Y);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Image display error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateControlSize()
        {
            viewModel.SetImageControlSize(new System.Windows.Size(image.ActualWidth, image.ActualHeight));

        }

        private void image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateControlSize();
        }
    }
}