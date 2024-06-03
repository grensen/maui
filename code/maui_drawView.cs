// https://www.youtube.com/watch?v=7rw13_a5GR0
// https://github.com/jfversluis/MauiDrawingViewSample

// .NET MAUI NO-XAML MARKUP
// install NuGet packages: 
// 1. CommunityToolkit.Maui
// 2. CommunityToolkit.Maui.Markup
// 3. CommunityToolkit.Maui.Core 
// 4. SkiaSharp

using System.Collections.ObjectModel;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Core;
using SkiaSharp;

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
    int dim = 22;

    public MainPage()
    {
        Image imageView = new() { WidthRequest = 100, HeightRequest = 100 };
        Image imageBitmap = new() { WidthRequest = 28, HeightRequest = 28 };

        DrawingView dv = new DrawingView
        {
            Lines = new ObservableCollection<IDrawingLine>(),
            BackgroundColor = Colors.Black,
            WidthRequest = 300,
            HeightRequest = 300,
            LineColor = Colors.White,
            LineWidth = 25,
        }
        .CenterHorizontal();

        dv.DrawingLineCompleted += async (s, e) =>
            await ProcessDrawing(s as DrawingView, imageBitmap, imageView, dim);

        VerticalStackLayout stack = new()
        {
            Spacing = 8,
            Padding = 15,
            Children =
            {      
                // draw user input
                CreateLabel("MAUI Markup Drawing Demo", 18),
                dv,
                CreateLabel($"Pen size {dv.LineWidth}", 12).Assign(out Label pen),
                CreateSlider(0, 1, 0.5f, 200, e =>
                {
                    dv.LineWidth = (1 + ((float)e.NewValue * (50 - 1)));
                    pen.Text = $"Pen size {dv.LineWidth:F0}";
                }),

                // draw stream output
                CreateLabel("Stream output", 12),
                imageView,
                                                  
                // draw final image
                CreateLabel("28 x 28 output", 12),
                imageBitmap,

                // slider final image dimension
                CreateLabel($"Dimension {dim}", 12).Assign(out Label desiredLabel),
                CreateSlider(0, 1, 0.8f, 200, e =>
                {
                    dim = (int)(5 + (float)e.NewValue * (28 - 5));
                    desiredLabel.Text = $"Dimension {dim}";
                }),
            }
        };

        // Set the main page content to the ScrollView
        Content = new ScrollView { Content = stack };
    }

    public static async Task ProcessDrawing(DrawingView? drawView, Image imageBitmap, Image imageView, int dim)
    {
        if (drawView != null && drawView.Lines != null)
        {
            // Get the image stream from drawView
            using var stream = await DrawingView.GetImageStream(
                drawView.Lines, new Size(drawView.Width, drawView.Height), Colors.Black);

            // Decode the stream into an SKBitmap
            using var bitmap = SKBitmap.Decode(stream);

            // Calculate the aspect ratio of the drawing
            float ratio = (float)bitmap.Width / bitmap.Height;

            // Calculate new dimensions while maintaining aspect ratio
            int newWidth = ratio > 1 ? dim : (int)(dim * ratio);
            int newHeight = ratio > 1 ? (int)(dim / ratio) : dim;

            // Resize the original bitmap to match the new dimensions
            using var resizedBitmap = bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);

            // Create a new 28x28 bitmap and fill it with a black background
            using var finalBitmap = new SKBitmap(28, 28);

            using (var canvas = new SKCanvas(finalBitmap))
            {
                canvas.Clear(SKColors.Black); // Set the background color to black (0s)
                                              
                // Calculate the position to draw the resized image to center it
                int x = (28 - newWidth) / 2, y = (28 - newHeight) / 2;
                canvas.DrawBitmap(resizedBitmap, x, y);
            }

            // Convert the centered and resized bitmap to grayscale and prepare the byte array
            float[] image28x28 = new float[28 * 28];
            for (int y = 0; y < 28; y++)
                for (int x = 0; x < 28; x++)
                {
                    var color = finalBitmap.GetPixel(x, y);
                    var luminance = (byte)((color.Red + color.Green + color.Blue) / 3);
                    image28x28[y * 28 + x] = luminance / 255.0f;

                    finalBitmap.SetPixel(x, y, new SKColor(luminance, luminance, luminance));
                }

            // Convert the SKBitmap to a .NET MAUI ImageSource
            imageBitmap.Source = ImageSource.FromStream(() =>
            {
                SKData data = SKImage.FromBitmap(finalBitmap).Encode(SKEncodedImageFormat.Png, 100);
                return data.AsStream();
            });

            // Set the imageFromView.Source from the original stream
            var streamForView = await DrawingView.GetImageStream(drawView.Lines, new Size(200, 200), Colors.Black);
            imageView.Source = ImageSource.FromStream(() => streamForView);
        }
    }

    public static Slider CreateSlider(float min, float max, float value, int widthRequest, Action<ValueChangedEventArgs> onValueChanged)
    {
        var slider = new Slider
        {
            Minimum = min,
            Maximum = max,
            Value = value,
            WidthRequest = widthRequest
        };
        slider.ValueChanged += (sender, e) => onValueChanged(e);
        return slider;
    }

    public static Label CreateLabel(string str, int sz) => new Label { Text = str }.Font(size: sz).CenterHorizontal();
}

/*
public class MainPage : ContentPage
{
    int desiredDim = 22;
    public MainPage()
    {
        Image imageFromView = new Image
        {
            WidthRequest = 100,
            HeightRequest = 100,
        };
        Image imageFromBitmap = new Image
        {
            WidthRequest = 28,
            HeightRequest = 28,
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 2, Padding = 5,
                Children =
                {
                    new Label { Text = "Hello MAUI Markup ML", }
                    .Font(size: 18).CenterHorizontal(),

                    new DrawingView
                    {
                        Lines = new ObservableCollection<CommunityToolkit.Maui.Core.IDrawingLine>(),
                        BackgroundColor = Colors.Black,
                        WidthRequest = 200,
                        HeightRequest = 200,
                        LineColor = Colors.White,
                        LineWidth = 25,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill,
                    }
                    .CenterHorizontal()
                    .Invoke(b => b.DrawingLineCompleted += async (sender, e) =>
                    {

                        DrawingView? drawView = sender as DrawingView;

                        var stream = await DrawingView.GetImageStream(drawView.Lines, new Size(300, 300), Colors.Black);
                        imageFromView.Source = ImageSource.FromStream(() => stream);

                        // Define the desired width and height
                        int desiredWidth = desiredDim;
                        int desiredHeight = desiredDim;

                        // Get the image stream from drawView
                        using var stream2 = await DrawingView.GetImageStream(drawView.Lines, new Size(drawView.Width, drawView.Height), Colors.Black);

                        // Decode the stream into an SKBitmap
                        using var originalBitmap = SKBitmap.Decode(stream2);

                        // Calculate the aspect ratio of the drawing
                        float aspectRatio = (float)originalBitmap.Width / originalBitmap.Height;

                        // Calculate new dimensions while maintaining aspect ratio
                        int newWidth, newHeight;
                        if (aspectRatio > 1) // Width is greater than height
                        {
                            newWidth = desiredWidth;
                            newHeight = (int)(desiredWidth / aspectRatio);
                        }
                        else // Height is greater than or equal to width
                        {
                            newHeight = desiredHeight;
                            newWidth = (int)(desiredHeight * aspectRatio);
                        }

                        // Resize the original bitmap to match the new dimensions
                        using var resizedBitmap = originalBitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);

                        // Create a new 28x28 bitmap and fill it with a black background
                        using var finalBitmap = new SKBitmap(28, 28);
                        using (var canvas = new SKCanvas(finalBitmap))
                        {
                            canvas.Clear(SKColors.Black); // Set the background color to black

                            // Calculate the position to draw the resized image to center it
                            int x = (28 - newWidth) / 2;
                            int y = (28 - newHeight) / 2;
                            canvas.DrawBitmap(resizedBitmap, x, y);
                        }

                        // Convert the centered and resized bitmap to grayscale and prepare the byte array
                        byte[] raw28x28image = new byte[28 * 28];
                        for (int y = 0; y < 28; y++)
                            for (int x = 0; x < 28; x++)
                            {
                                var color = finalBitmap.GetPixel(x, y);
                                var luminance = (byte)(0.2126 * color.Red + 0.7152 * color.Green + 0.0722 * color.Blue);
                                raw28x28image[y * 28 + x] = luminance;

                                finalBitmap.SetPixel(x, y, new SKColor(luminance, luminance, luminance));
                            }

                        // Prepare the array for the neural network prediction
                        float[] sam = new float[28 * 28];
                        for (int i = 0; i < sam.Length; i++)
                            sam[i] = raw28x28image[i] / 255.0f;

                        // Convert the SKBitmap to a .NET MAUI ImageSource
                        imageFromBitmap.Source = ImageSource.FromStream(() =>
                        {
                            SKData data = SKImage.FromBitmap(finalBitmap).Encode(SKEncodedImageFormat.Png, 100);
                            return data.AsStream();
                        });

                    }).Assign(out DrawingView drawView),

                    new Label { Text = $"Pen size {drawView.LineWidth}" }
                    .CenterHorizontal().Assign(out Label penLabel),
                    new Slider { Minimum = 0, Maximum = 1, Value = 0.5, WidthRequest = 200 }
                    .Invoke(b => b.ValueChanged  += (sender, e) =>
                    {
                        drawView.LineWidth = (1 + ((float)e.NewValue * (50 - 1)));
                        penLabel.Text = $"Pen size {drawView.LineWidth:F0}"; //String.Format($"The Slider value is {drawView.LineWidth,-7}", e.NewValue);
                    }),

                    new Label { Text = $"Dimension {desiredDim}"}
                    .CenterHorizontal().Assign(out Label desiredLabel),
                    new Slider { Minimum = 0, Maximum = 1, Value = 0.8, WidthRequest = 200 }
                    .Invoke(b => b.ValueChanged  += (sender, e) =>
                    {
                        desiredDim = (int)(0 + (float)e.NewValue * (28 - 0));
                        desiredLabel.Text = $"Dimension {desiredDim = (int)(0 + (float)e.NewValue * (28 - 0)),-7}";
                    }),

                    imageFromView,

                    imageFromBitmap
                }
            }
        };
    }
}
*/
