using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PetDesk.Core.Models;

namespace PetDesk.AssetBuilder;

internal static class Program
{
    private static readonly Dictionary<string, Brush> Brushes = new();
    private static Brush B(string color)
    {
        if (!Brushes.TryGetValue(color, out var b))
        {
            b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            b.Freeze();
            Brushes.Add(color, b);
        }

        return b;
    }

    private static Pen P(string color, double width = 2) => new(B(color), width)
    {
        StartLineCap = PenLineCap.Round,
        EndLineCap = PenLineCap.Round,
        LineJoin = PenLineJoin.Round
    };
    private static void Shape(DrawingContext d, string color, string path, string? stroke = null, double thickness = 2) => d.DrawGeometry(B(color), stroke is null ? null : P(stroke, thickness), Geometry.Parse(path));
    private static void Ellipse(DrawingContext d, string color, double x, double y, double rx, double ry, string? stroke = null) => d.DrawEllipse(B(color), stroke is null ? null : P(stroke), new Point(x, y), rx, ry);
    [STAThread]
    public static void Main(string[] args)
    {
        string root = args.FirstOrDefault() ?? Path.GetFullPath("assets/builtin");
        Create(root, "mochi-cat", "Mochi Cat", DrawCat);
        Create(root, "pixel-fox", "Pixel Fox", DrawFox);
        if (args.Length > 1)
        {
            CreateIcon(args[1]);
        }
    }

    private static void CreateIcon(string path)
    {
        var visual = new DrawingVisual();
        using (var d = visual.RenderOpen())
        {
            var gradient = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#9844C3"), (Color)ColorConverter.ConvertFromString("#382047"), 55);
            d.DrawRoundedRectangle(gradient, null, new(0, 0, 64, 64), 15, 15);
            Ellipse(d, "#F4B4FF", 14, 25, 6, 8);
            Ellipse(d, "#F4B4FF", 26, 17, 6, 8);
            Ellipse(d, "#F4B4FF", 41, 18, 6, 8);
            Ellipse(d, "#F4B4FF", 52, 29, 5, 7);
            Shape(d, "#F4B4FF", "M17,46 C17,35 27,29 33,30 C41,30 48,41 48,47 C48,56 40,53 33,51 C25,54 17,55 17,46 Z");
        }

        var image = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
        image.Render(visual);
        using var data = new MemoryStream();
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(image));
        png.Save(data);
        using var output = new BinaryWriter(File.Create(path));
        output.Write((ushort)0);
        output.Write((ushort)1);
        output.Write((ushort)1);
        output.Write((byte)64);
        output.Write((byte)64);
        output.Write((byte)0);
        output.Write((byte)0);
        output.Write((ushort)1);
        output.Write((ushort)32);
        output.Write((uint)data.Length);
        output.Write((uint)22);
        output.Write(data.ToArray());
    }

    private static void Create(string root, string id, string name, Action<DrawingContext, int, int> draw)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
            for (int row = 0; row < 3; row++)
            {
                for (int frame = 0; frame < (row == 0 ? 4 : 6); frame++)
                {
                    dc.PushTransform(new TranslateTransform(frame * 192, row * 208));
                    if (row == 2)
                    {
                        dc.PushTransform(new MatrixTransform(-1, 0, 0, 1, 192, 0));
                    }

                    draw(dc, row == 0 ? 0 : 1, frame);
                    if (row == 2)
                    {
                        dc.Pop();
                    }

                    dc.Pop();
                }
            }

        var bitmap = new RenderTargetBitmap(1152, 624, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        string directory = Path.Combine(root, id);
        Directory.CreateDirectory(directory);
        using (var output = File.Create(Path.Combine(directory, "spritesheet.png")))
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(output);
        }

        var manifest = new PetManifest
        {
            Id = "builtin-" + id,
            Name = name,
            FrameWidth = 192,
            FrameHeight = 208,
            Columns = 6,
            Rows = 3,
            Animations = new()
            {
                ["idle"] = new()
                {
                    Row = 0,
                    Frames = 4,
                    Fps = 4
                },
                ["walkRight"] = new()
                {
                    Row = 1,
                    Frames = 6,
                    Fps = 8
                },
                ["walkLeft"] = new()
                {
                    Row = 2,
                    Frames = 6,
                    Fps = 8
                }
            }
        };
        File.WriteAllText(Path.Combine(directory, "pet.json"), JsonSerializer.Serialize(manifest, PetJson.Options));
        Console.WriteLine($"{name}: {bitmap.PixelWidth}x{bitmap.PixelHeight}, idle + left/right walk");
    }

    private static void DrawCat(DrawingContext d, int state, int frame)
    {
        double phase = frame * Math.PI * 2 / (state == 0 ? 4 : 6), lift = state == 0 ? Math.Sin(phase) * 1.7 : Math.Abs(Math.Sin(phase)) * -3;
        Shape(d, "#E7EAF3", "M128,150 C167,127 185,141 171,164 C163,180 143,173 133,169 Z", "#BBC4D8", 3);
        d.PushTransform(new TranslateTransform(0, lift));
        Ellipse(d, "#E9ECF5", 96, 146, 39, 37, "#BBC4D8");
        for (int leg = 0; leg < 4; leg++)
        {
            double step = state == 1 ? Math.Sin(phase + leg * Math.PI) * 7 : 0;
            Ellipse(d, "#F8FAFF", 67 + leg * 19 + step, 179 - Math.Max(0, -step) * .4, 11, 8, "#BBC4D8");
        }

        Shape(d, "#F8FAFF", "M48,79 L45,37 Q46,28 55,36 L81,57 M112,57 L139,35 Q148,29 148,40 L145,82 Z", "#BBC4D8", 3);
        Shape(d, "#EDB8CC", "M53,65 L52,41 L74,60 Z M121,59 L140,41 L140,68 Z");
        Ellipse(d, "#F8FAFF", 96, 94, 54, 43, "#BBC4D8");
        Ellipse(d, "#F8FAFF", 50, 105, 12, 13);
        Ellipse(d, "#F8FAFF", 142, 105, 12, 13);
        bool blink = state == 0 && frame == 2;
        foreach (double eye in new[]
        {
            75d,
            116d
        }

        )
        {
            if (blink)
            {
                d.DrawLine(P("#34405A", 4), new(eye - 7, 94), new(eye + 7, 94));
            }
            else
            {
                Ellipse(d, "#34405A", eye, 91, 10, 13);
                Ellipse(d, "#93B3CA", eye + 1, 94, 5, 6);
                Ellipse(d, "#FFFFFF", eye - 3, 86, 3.3, 4);
            }
        }

        Ellipse(d, "#F6D7E3", 59, 105, 9, 4);
        Ellipse(d, "#F6D7E3", 132, 105, 9, 4);
        Shape(d, "#DE98B6", "M90,104 Q96,100 102,104 L96,111 Z");
        d.DrawGeometry(null, P("#6A617D", 2), Geometry.Parse("M96,110 Q90,119 84,113 M96,110 Q102,119 108,113"));
        d.DrawRoundedRectangle(B("#AA96DA"), null, new(64, 127, 64, 10), 5, 5);
        Ellipse(d, "#F5D990", 96, 140, 7, 8, "#BDA35E");
        d.Pop();
    }

    private static void DrawFox(DrawingContext d, int state, int frame)
    {
        double phase = frame * Math.PI * 2 / (state == 0 ? 4 : 6);
        int bob = state == 1 ? (frame % 3 == 1 ? -1 : 0) : (frame % 2);
        void R(string color, int x, int y, int w, int h) => d.DrawRectangle(B(color), null, new(x * 4, (y + bob) * 4, w * 4, h * 4));
        R("#633641", 33, 27, 10, 10);
        R("#DA6A39", 33, 28, 9, 8);
        R("#FF9D4C", 37, 25, 6, 8);
        R("#FFF0CE", 40, 22, 5, 8);
        R("#633641", 13, 28, 21, 15);
        R("#DD6F39", 14, 28, 18, 14);
        R("#F58B41", 16, 28, 14, 12);
        R("#FFF0CE", 15, 30, 8, 10);
        for (int leg = 0; leg < 4; leg++)
        {
            int stride = state == 1 ? (int)Math.Round(Math.Sin(phase + leg * Math.PI) * 2) : 0;
            R("#633641", 15 + leg * 5 + stride, 41, 4, 5 - Math.Max(0, stride));
            R("#443044", 15 + leg * 5 + stride, 44 - Math.Max(0, stride), 4, 3);
        }

        R("#633641", 9, 10, 7, 14);
        R("#633641", 29, 10, 7, 14);
        R("#F58B41", 10, 12, 5, 11);
        R("#F58B41", 30, 12, 5, 11);
        R("#E6A199", 12, 15, 3, 7);
        R("#E6A199", 30, 15, 3, 7);
        R("#633641", 9, 19, 27, 14);
        R("#F58B41", 10, 18, 25, 14);
        R("#FFB65C", 13, 18, 19, 9);
        R("#FFF0CE", 10, 27, 25, 5);
        R("#FFF0CE", 15, 31, 15, 4);
        R("#633641", 20, 29, 5, 3);
        bool blink = state == 0 && frame == 2;
        R("#443044", 15, 23, 3, blink ? 1 : 4);
        R("#443044", 27, 23, 3, blink ? 1 : 4);
        if (!blink)
        {
            R("#FFFFFF", 15, 23, 1, 1);
            R("#FFFFFF", 27, 23, 1, 1);
        }

        R("#C398DB", 16, 35, 14, 2);
    }
}
