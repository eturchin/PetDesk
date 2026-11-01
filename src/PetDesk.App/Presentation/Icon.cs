using System.Windows.Documents;

namespace PetDesk.App.Presentation;

public sealed class Icon : FrameworkElement
{
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(nameof(Kind), typeof(string), typeof(Icon), new FrameworkPropertyMetadata("Paw", FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(typeof(Icon), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));
    public string Kind { get => (string)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public Brush Foreground { get => (Brush)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    private static readonly Dictionary<string, Geometry> Shapes = CreateShapes();
    private static Dictionary<string, Geometry> CreateShapes()
    {
        var paths = new Dictionary<string, string>
        {
            ["Paw"] = "M6,17 C6,13 10,10 12,10 C14,10 18,13 18,17 C18,20 15,20 12,19 C9,20 6,20 6,17 Z M4,8 A2,3 0 1 0 8,8 A2,3 0 1 0 4,8 M9,5 A2,3 0 1 0 13,5 A2,3 0 1 0 9,5 M15,6 A2,3 0 1 0 19,6 A2,3 0 1 0 15,6 M19,11 A2,3 0 1 0 23,11 A2,3 0 1 0 19,11",
            ["Eye"] = "M2,12 Q12,-2 22,12 Q12,26 2,12 Z M8,12 A4,4 0 1 0 16,12 A4,4 0 1 0 8,12",
            ["Free"] = "M3,16 C3,4 11,4 11,12 C11,21 19,20 19,8 M16,10 L19,7 22,10",
            ["Pin"] = "M12,22 C10,18 4,14 4,9 A8,8 0 0 1 20,9 C20,14 14,18 12,22 Z M9,9 A3,3 0 1 0 15,9 A3,3 0 1 0 9,9",
            ["Monitor"] = "M3,4 H21 V17 H3 Z M12,17 V21 M7,21 H17",
            ["Move"] = "M12,2 V22 M2,12 H22 M8,6 L12,2 16,6 M8,18 L12,22 16,18 M6,8 L2,12 6,16 M18,8 L22,12 18,16",
            ["Expand"] = "M3,9 V3 H9 M15,3 H21 V9 M21,15 V21 H15 M9,21 H3 V15 M3,21 L10,14 M14,10 L21,3",
            ["Drop"] = "M12,2 C9,7 4,11 4,16 A8,7 0 0 0 20,16 C20,11 15,7 12,2 Z M8,15 Q7,18 11,19",
            ["Gauge"] = "M4,19 A10,10 0 1 1 20,19 M6,17 H6.1 M5,10 H5.1 M12,5 V5.1 M19,10 H19.1 M12,15 L17,9 M10,15 A2,2 0 1 0 14,15 A2,2 0 1 0 10,15",
            ["Globe"] = "M2,12 A10,10 0 1 0 22,12 A10,10 0 1 0 2,12 M2,12 H22 M12,2 C6,7 6,17 12,22 C18,17 18,7 12,2 Z",
            ["Gear"] = "M10,2 H14 L15,5 18,6 21,6 23,10 20,12 20,15 21,18 18,21 15,20 12,22 9,20 6,21 3,18 4,15 3,12 1,10 3,6 6,6 9,5 Z M8,12 A4,4 0 1 0 16,12 A4,4 0 1 0 8,12",
            ["Dots"] = "M4,12 H4.1 M12,12 H12.1 M20,12 H20.1",
            ["Back"] = "M14,4 L6,12 14,20 M6,12 H22",
            ["Plus"] = "M12,4 V20 M4,12 H20",
            ["Folder"] = "M2,6 V20 H22 V7 H11 L8,4 H2 Z",
            ["Trash"] = "M3,6 H21 M9,6 V3 H15 V6 M6,6 L7,21 H17 L18,6 M10,10 V17 M14,10 V17",
            ["Check"] = "M4,12 L9,17 20,6",
            ["Close"] = "M6,6 L18,18 M6,18 L18,6",
            ["Chevron"] = "M5,9 L12,16 19,9",
            ["Info"] = "M2,12 A10,10 0 1 0 22,12 A10,10 0 1 0 2,12 M12,10 V17 M12,6 V6.1",
            ["Reset"] = "M3,10 A9,9 0 1 1 4,18 M3,3 V10 H10",
            ["Palette"] = "M12,2 A10,10 0 1 0 12,22 C16,22 11,18 16,16 C24,18 25,4 12,2 Z M6,10 H6.1 M10,6 H10.1 M16,7 H16.1 M18,12 H18.1"
        };
        return paths.ToDictionary(p => p.Key, p =>
        {
            var geometry = Geometry.Parse(p.Value);
            geometry.Freeze();
            return geometry;
        });
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (!Shapes.TryGetValue(Kind, out var shape))
        {
            return;
        }

        var scale = Math.Min(ActualWidth, ActualHeight) / 24;
        dc.PushTransform(new TranslateTransform((ActualWidth - 24 * scale) / 2, (ActualHeight - 24 * scale) / 2));
        dc.PushTransform(new ScaleTransform(scale, scale));
        var pen = new Pen(Foreground, Kind == "Dots" ? 4 : 1.8)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        dc.DrawGeometry(Kind == "Paw" ? Foreground : null, Kind == "Paw" ? null : pen, shape);
        dc.Pop();
        dc.Pop();
    }
}
