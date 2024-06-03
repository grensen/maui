// .NET MAUI NO-XAML MARKUP

// 1. create maui app with name "tempmaui"
// 2. delete: App.xaml, AppShell.xaml, MainPage.xaml, MauiProgram.cs
// 3. create new class and name it "tempmaui"

// Add Android permissions (Platforms/Android/Resources/AndroidManifest.xml)
// <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
// <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />

// install NuGet packages: 
// 1. CommunityToolkit.Maui
// 2. CommunityToolkit.Maui.Markup
// 3. CommunityToolkit.Maui.Core 
// 4. SkiaSharp

using System.Collections.ObjectModel;
using System.Globalization;
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
    // public App() => MainPage = new MainPage();
    public App()
    {
        MainPage = new MainPage();
    }

    public void ChangeMainPage(string[] nn)
    {
        MainPage = new PredictionPage(nn);
    }
}

public class MainPage : ContentPage
{
    private ProgressBar progressBar;
    private Button downloadButton;
    const string url = "https://raw.githubusercontent.com/grensen/how_to_train/main/system1.txt";
    const string netName = "system1.txt";
    string filePath = Path.Combine(FileSystem.AppDataDirectory, netName);

    public MainPage()
    {
        progressBar = new ProgressBar { IsVisible = false, HeightRequest = 20, Margin = new Thickness(0, 10) };

        downloadButton = new Button {
            Text = "Download File", 
            BackgroundColor = Colors.DarkSlateBlue, 
            TextColor = Colors.White, 
            CornerRadius = 5, 
            WidthRequest = 200 };

        downloadButton.Clicked += async (s, e) => await StartDownloadProcessAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Children = {
                    new Label { Text = $"URL: {url}\n" +  $"Path: {filePath}"
                    , FontSize = 10, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 10) },
                    downloadButton,
                    progressBar
                }
            }
        };
    }

    private async Task StartDownloadProcessAsync()
    {
        downloadButton.IsEnabled = false;
        progressBar.IsVisible = true;
        progressBar.Progress = 0;

        await DownloadFileAsync();

        var lines = await File.ReadAllLinesAsync(filePath);

        ((App)Application.Current).ChangeMainPage(lines);
    }

    private async Task DownloadFileAsync()
    {
        using HttpClient client = new();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var memoryStream = new MemoryStream();
        await using var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[8192];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer)) != 0)
        {
            await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            if (totalBytes > 0)
                progressBar.Progress = (double)memoryStream.Length / totalBytes;
        }

        await SaveMemoryStreamToFileAsync(memoryStream);

        async Task SaveMemoryStreamToFileAsync(MemoryStream memoryStream)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await memoryStream.CopyToAsync(fileStream);
        }
    }
}

class NeuralNetwork // neural net class
{
    public float[] weight = null;
    public int[] net = null;
    public int prediction = 0;
    public int len = 0;
    public string name;
    public NeuralNetwork(string[] nn) // optional parameter
    {
        if (nn.Length > 0)
        {
            // make sure download is done before call here:
            this.net = nn[0].Split(',').Select(int.Parse).ToArray();
            this.name = nn[0];
            // init net
            this.len = this.net.Sum(); // neurons len (in+hidden+out)
            this.weight = new float[nn.Length - 1]; // network parameters
            this.prediction = 0;
            // load trained weights
            for (int n = 1; n < nn.Length; n++)
            {
                // for android and windows needed
                if (float.TryParse(nn[n], NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                {
                    this.weight[n - 1] = result;
                }
                else
                {
                    throw new FormatException($"Unable to parse '{nn[n]}' as a float.");
                }
            }
        }
    }

    public static int Predict(ref NeuralNetwork nn, Span<float> sample)
    {
        Span<float> neuron = new float[nn.len];
        sample.CopyTo(neuron);
        FeedForward(neuron, nn.net, nn.weight);
        var outs = neuron.Slice(neuron.Length - nn.net[^1], nn.net[^1]);
        nn.prediction = Argmax(outs);
        return nn.prediction;
    }

    public static int Argmax(Span<float> neurons)
    {
        int id = 0;
        for (int i = 1; i < neurons.Length; i++)
            if (neurons[i] > neurons[id])
                id = i;
        return id; // prediction
    }

    public static void FeedForward(Span<float> neurons, Span<int> net, Span<float> weights)
    {
        for (int i = 0, j = 0, k = net[0], w = 0; i < net.Length - 1; i++) // each layer
        {
            int left = net[i], right = net[i + 1];
            for (int l = 0; l < left; l++, w += right) // input neurons
            {
                float n = neurons[j + l];
                if (n > 0) // ReLU pre-activation
                    for (int r = 0; r < right; r++) // output neurons
                        neurons[k + r] += n * weights[w + r];
            }
            j += left; k += right;
        }
    }
}

public class PredictionPage : ContentPage
{
    int dim = 22;
    Image imageView;
    Image imageBitmap;
    DrawingView dv;
    public Label samplePrediction;
    NeuralNetwork neuralNetwork;

    public PredictionPage(string[] nn)
    {
        neuralNetwork = new NeuralNetwork(nn);

        samplePrediction = new Label();
        samplePrediction.HorizontalOptions = LayoutOptions.Center;

        imageView = new Image { WidthRequest = 100, HeightRequest = 50 };
        imageBitmap = new Image { WidthRequest = 28, HeightRequest = 28 };

        dv = new DrawingView
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
                CreateLabel("DrawingView Neural Network: " +neuralNetwork.name , 14),
                dv,

                samplePrediction,
                // draw stream output
                CreateLabel("Stream output", 12),
                imageView,
                                                  
                // draw final image
                CreateLabel("28 x 28 output", 12),
                imageBitmap,

                // slider DrawingView Pen size
                CreateLabel($"Pen size {dv.LineWidth}", 12).Assign(out Label pen),
                CreateSlider(0, 1, 0.5f, 200, e =>
                {
                    dv.LineWidth = (1 + ((float)e.NewValue * (50 - 1)));
                    pen.Text = $"Pen size {dv.LineWidth:F0}";
                }),

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

    public async Task ProcessDrawing(
        DrawingView? drawView, Image imageBitmap, Image imageView, int dim)
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

            // predict sample with trained neural net
            int p = NeuralNetwork.Predict(ref neuralNetwork, image28x28);
            samplePrediction.Text = $"Prediction is {p}";

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
