using Microsoft.VisualStudio.TestTools.UnitTesting;
using NImageViewer.Helper;
using System;
using System.Windows;

namespace NImageViewerTest.Helper
{
    [TestClass]
    public class ImageHelperTest
    {
        [TestMethod]
        public void AlphaComposite()
        {
            var c1 = System.Windows.Media.Color.FromArgb(255, 50, 50, 50);
            var c2 = System.Windows.Media.Color.FromArgb(51, 10, 10, 10);
            var newColor = ImageHelper.AlphaComposite(c1, c2);
            Assert.AreEqual(System.Windows.Media.Color.FromArgb(255, 42, 42, 42), newColor);

            c1 = System.Windows.Media.Color.FromArgb(255, 50, 50, 50);
            c2 = System.Windows.Media.Color.FromArgb(255, 10, 10, 10);
            newColor = ImageHelper.AlphaComposite(c1, c2);
            Assert.AreEqual(System.Windows.Media.Color.FromArgb(255, 10, 10, 10), newColor);
        }

        [TestMethod]
        public void CenterImage()
        {
            double height = 2000;
            double width = 1000;
            var position = new Point(500, 1000);

            double scale = 1.1;
            var location1 = Scale(0, 0, width, height, position, scale);
            Assert.AreEqual(new Point(-50, -100), location1);

            position = new Point(250, 500);
            location1 = Scale(0, 0, width, height, position, scale);
            Assert.AreEqual(new Point(-25, -50), location1);

        }

        private System.Windows.Point Scale(double x, double y, double width, double height, Point mousePosition, double scale)
        {
            return ImageHelper.OffsetScaledRectangleOnMousePosition(new System.Windows.Rect(x, y, width, height), scale, mousePosition);
        }
    }
}