using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace NImageViewer.Controls
{
    /// <summary>
    /// Represents a decorator for scroll viewer and its content. Adds ability to DnD the content element. 
    /// When it is dragged the scroll bar offsets are updated and the content is moved as the mouse pointer moves.
    /// </summary>
    public class ScrollDragger
    {
        private readonly ScrollViewer scrollViewer;
        private readonly UIElement content;
        private Point scrollMousePoint;
        private double hOff = 1;
        private double vOff = 1;

        public ScrollDragger(UIElement content, ScrollViewer scrollViewer)
        {
            this.scrollViewer = scrollViewer;
            this.content = content;
            content.MouseLeftButtonDown += scrollViewer_MouseLeftButtonDown;
            content.PreviewMouseMove += scrollViewer_PreviewMouseMove;
            content.PreviewMouseLeftButtonUp += scrollViewer_PreviewMouseLeftButtonUp;
        }

        public bool IsEnabled { get; set; } = true;

        private void scrollViewer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            content.CaptureMouse();
            scrollMousePoint = e.GetPosition(scrollViewer);
            hOff = scrollViewer.VerticalOffset;
            vOff = scrollViewer.HorizontalOffset;
        }

        private void scrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (content.IsMouseCaptured)
            {
                var newYOffset = hOff + (scrollMousePoint.Y - e.GetPosition(scrollViewer).Y);
                scrollViewer.ScrollToVerticalOffset(newYOffset);
                var newXOffset = vOff + (scrollMousePoint.X - e.GetPosition(scrollViewer).X);
                scrollViewer.ScrollToHorizontalOffset(newXOffset);
            }
        }

        private void scrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            content.ReleaseMouseCapture();
        }
    }
}
