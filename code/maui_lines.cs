// .NET MAUI NO-XAML MARKUP
// install NuGet packages: 
// 1. CommunityToolkit.Maui
// 2. CommunityToolkit.Maui.Markup

using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;

namespace tempmaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMarkup()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        return builder.Build();
    }
}

public class App : Application 
{ 
    public App() => MainPage = new MainPage(); 
}

public class MainPage : ContentPage
{
    public MainPage()
    {
        GraphicsView gView = new()
        { 
            Drawable = new DrawLines() 
        };

        gView.SizeChanged += 
            (sender, e) => gView.Invalidate();

        Content = gView;
    }
}

public class DrawLines : IDrawable
{
    public void Draw(ICanvas canvas, RectF rect)
    {
        float width = rect.Width;
        float height = rect.Height;
        var rng = new Random();

        for (int i = 0; i < 200; i++)
        {
            var startX = rng.NextSingle() * width;
            var startY = rng.NextSingle() * height;
            var point1X = rng.NextSingle() * width;
            var point1Y = rng.NextSingle() * height;
            var point2X = rng.NextSingle() * width;
            var point2Y = rng.NextSingle() * height;
            var endX = rng.NextSingle() * width;
            var endY = rng.NextSingle() * height;

            int r = rng.Next(256);
            int g = rng.Next(256);
            int b = rng.Next(256);

            canvas.StrokeColor = Color.FromRgb(r, g, b);
            canvas.StrokeSize = rng.Next(4, 20);
            canvas.StrokeLineCap = LineCap.Round;

            var p = new PathF();
            p.MoveTo(startX, startY);
            p.CurveTo(point1X, point1Y, point2X, point2Y, endX, endY);
            canvas.DrawPath(p);
        }
    }
}