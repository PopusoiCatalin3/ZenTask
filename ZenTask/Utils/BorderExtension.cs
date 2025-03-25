using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZenTask.Utils
{
    /// <summary>
    /// Adaugă proprietăți atașate pentru a putea seta border-ul doar pe partea stângă
    /// </summary>
    public static class BorderExtension
    {
        #region BorderLeft

        public static readonly DependencyProperty BorderLeftProperty =
            DependencyProperty.RegisterAttached(
                "BorderLeft",
                typeof(Brush),
                typeof(BorderExtension),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnBorderLeftChanged));

        public static Brush GetBorderLeft(DependencyObject obj)
        {
            return (Brush)obj.GetValue(BorderLeftProperty);
        }

        public static void SetBorderLeft(DependencyObject obj, Brush value)
        {
            obj.SetValue(BorderLeftProperty, value);
        }

        private static void OnBorderLeftChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Border border)
            {
                UpdateBorder(border);
            }
        }

        #endregion

        #region BorderLeftThickness

        public static readonly DependencyProperty BorderLeftThicknessProperty =
            DependencyProperty.RegisterAttached(
                "BorderLeftThickness",
                typeof(double),
                typeof(BorderExtension),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnBorderLeftThicknessChanged));

        public static double GetBorderLeftThickness(DependencyObject obj)
        {
            return (double)obj.GetValue(BorderLeftThicknessProperty);
        }

        public static void SetBorderLeftThickness(DependencyObject obj, double value)
        {
            obj.SetValue(BorderLeftThicknessProperty, value);
        }

        private static void OnBorderLeftThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Border border)
            {
                UpdateBorder(border);
            }
        }

        #endregion

        private static void UpdateBorder(Border border)
        {
            var leftBrush = GetBorderLeft(border);
            var leftThickness = GetBorderLeftThickness(border);

            // Dacă nu avem brush sau thickness, nu facem nimic
            if (leftBrush == null || leftThickness <= 0)
                return;

            // Setăm brush-ul pentru tot border-ul
            border.BorderBrush = border.BorderBrush ?? Brushes.Transparent;

            // Creăm un brush compus
            var brushes = new VisualBrush[4];
            brushes[0] = new VisualBrush { Visual = new Border { Background = leftBrush, Width = 1, Height = 1 } }; // Left
            brushes[1] = new VisualBrush { Visual = new Border { Background = border.BorderBrush, Width = 1, Height = 1 } }; // Top
            brushes[2] = new VisualBrush { Visual = new Border { Background = border.BorderBrush, Width = 1, Height = 1 } }; // Right
            brushes[3] = new VisualBrush { Visual = new Border { Background = border.BorderBrush, Width = 1, Height = 1 } }; // Bottom

            // Setăm thicknesses
            var thickness = border.BorderThickness;
            thickness = new Thickness(leftThickness, thickness.Top, thickness.Right, thickness.Bottom);
            border.BorderThickness = thickness;

            // Creăm un DrawingBrush pentru fiecare parte a border-ului
            var drawingGroup = new DrawingGroup();
            var borderPen = new Pen(brushes[0], leftThickness);

            drawingGroup.Children.Add(
                new GeometryDrawing(
                    Brushes.Transparent,
                    borderPen,
                    new RectangleGeometry(new Rect(0, 0, 1, 1))));

            border.BorderBrush = new DrawingBrush(drawingGroup);
        }
    }
}