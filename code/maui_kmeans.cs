using CommunityToolkit.Maui.Markup;
using SkiaSharp;

namespace tempmaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp() => MauiApp.CreateBuilder()
        .UseMauiApp<App>()
        .UseMauiCommunityToolkitMarkup()
        .Build();
}

public class App : Application
{
    public App() => MainPage = new MainPage();
}

public class MainPage : ContentPage
{
    SKBitmap bitmap = new(100, 100);
    Image imageSource, imageKmeans;

    int k = 2;
    int percentage = 10;
    bool random = true;

    public MainPage()
    {
        var vStack = new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label { Text = "MAUI K-Means Segmentation" }
                .Font(size: 16)
                .CenterHorizontal(),

                CreateButton("Pick Image")
                .Invoke(b => b.Clicked += async (sender, e) => await PickImage()),

                CreateButton("Perform K-Means")
                .Invoke(b => b.Clicked += (sender, e) => PerformKMeansBitmap(k, percentage, bitmap)),

                CreateButton("Random Colors").Assign(out Button buttonClr)
                .Invoke(b => b.Clicked += (sender, e) =>
                {
                    //var button = sender as Button;
                    //button.Text = (random = !random) ? "Random Colors" : "Ordered Colors";
                    buttonClr.Text = (random = !random) ? "Random Colors" : "Ordered Colors";
                }),

                CreateLabel($"K = {k:F0}", 10)
                .Assign(out Label kMeansLabel),                                
                // kmeans cluster size slider
                CreateSlider(0, 1, 0.0f, 240, e =>
                {
                    k = (int)(2 + e.NewValue * (50 - 2));
                    kMeansLabel.Text = $"K = {k:F0}";
                }),

                CreateLabel($"Image Size = {percentage:F0}%", 10)
                .Assign(out Label percLabel),
                // kmeans image size slider
                CreateSlider(0, 1, 0.1f, 240, e =>
                {
                    percentage = (int)(1 + e.NewValue * (100 - 1));
                    percLabel.Text = $"Image Size = {percentage:F0}%";
                }),

                // source image
                CreateLabel($"Source Image", 10),
                (imageSource = new Image
                {
                    WidthRequest = 300,
                    HeightRequest = 200,
                    BackgroundColor = Colors.Black
                }).CenterHorizontal(),

                // kmeans output
                CreateLabel($"K-Means Output", 10),
                (imageKmeans = new Image
                {
                    WidthRequest = 300,
                    HeightRequest = 200,
                    BackgroundColor = Colors.Black
                }).CenterHorizontal()
            }
        }.Paddings(8, 8, 8, 8);
        Content = new ScrollView { Content = vStack };
    }

    async Task PickImage()
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Pick an image",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                using var inputStream = File.OpenRead(result.FullPath);
                bitmap = SKBitmap.Decode(inputStream);
                imageKmeans.Source = ImageSource.FromFile(result.FullPath);
                imageSource.Source = imageKmeans.Source;
            };
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    Stream BitmapToStream(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return new MemoryStream(data.ToArray());
    }

    void PerformKMeansBitmap(int k, int percentage, SKBitmap original)
    {
        int newWidth = original.Width * percentage / 100;
        int newHeight = original.Height * percentage / 100;

        var resizedBitmap = original.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.Medium);

        int[][] pixelValues = ExtractColorValues(resizedBitmap);

        int[] labels = KMeans.Cluster(pixelValues, k);

        var clusteredImage = ClusteredImage(pixelValues, labels, newWidth, newHeight, k, random);

        // Convert the clustered image to a stream
        var imageStream = BitmapToStream(clusteredImage);

        // Set the imageKmeans source to the stream
        imageKmeans.Source = ImageSource.FromStream(() => imageStream);
    }

    int[][] ExtractColorValues(SKBitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        int[][] pixelValues = new int[width * height][];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                pixelValues[y * width + x] = [color.Red, color.Green, color.Blue];
            }
        return pixelValues;
    }

    SKBitmap ClusteredImage(int[][] pixelValues, int[] labels, int width, int height, int k, bool random)
    {
        SKBitmap clustered = new SKBitmap(width, height);

        if (random)
        {
            // Define random colors for clusters
            Random rng = new Random();
            SKColor[] clusterColors = new SKColor[k];
            for (int i = 0; i < k; i++)
                clusterColors[i] = new SKColor((byte)rng.Next(256), (byte)rng.Next(256), (byte)rng.Next(256));

            for (int y = 0, index = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int cluster = labels[index++];
                    SKColor color = clusterColors[cluster];
                    clustered.SetPixel(x, y, color);
                }
        }
        else
        {
            int[][] clusterSums = new int[k][];
            int[] clusterCounts = new int[k];
            for (int i = 0; i < k; i++) clusterSums[i] = new int[3];

            for (int i = 0; i < labels.Length; i++)
            {
                int cluster = labels[i];
                clusterSums[cluster][0] += pixelValues[i][0];
                clusterSums[cluster][1] += pixelValues[i][1];
                clusterSums[cluster][2] += pixelValues[i][2];
                clusterCounts[cluster]++;
            }

            SKColor[] clusterColors = new SKColor[k];
            for (int i = 0; i < k; i++)
                if (clusterCounts[i] > 0)
                {
                    clusterColors[i] = new SKColor(
                        (byte)(clusterSums[i][0] / clusterCounts[i]),
                        (byte)(clusterSums[i][1] / clusterCounts[i]),
                        (byte)(clusterSums[i][2] / clusterCounts[i])
                    );
                }
                else
                {
                    clusterColors[i] = SKColors.Black;
                }

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    clustered.SetPixel(x, y, clusterColors[labels[y * width + x]]);
        }

        return clustered;
    }

    static Button CreateButton(string text) => new Button
    {
        Text = text,
        FontSize = 12,
        WidthRequest = 160,
        BackgroundColor = Colors.RoyalBlue,
        HeightRequest = 35
    }
    .Font(bold: true)
    .CenterHorizontal();

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

    public static Label CreateLabel(string str, int sz) =>
        new Label { Text = str }.Font(size: sz).CenterHorizontal();
}

public class KMeans
{
    private static Random random = new Random();
    public static int[] Cluster(int[][] data, int k)
    {
        const int maxIterations = 50;
        int[][] centroids = InitializeCentroids(data, k);
        int[] labels = new int[data.Length];
        bool hasChanged;

        for (int iterations = 0; iterations < maxIterations; iterations++)
        {
            hasChanged = false;

            for (int i = 0; i < data.Length; i++)
            {
                int newLabel = FindClosestCentroid(data[i], centroids);
                if (newLabel != labels[i])
                {
                    labels[i] = newLabel;
                    hasChanged = true;
                }
            }

            if (!hasChanged) break;

            centroids = UpdateCentroids(data, labels, k);
        }

        return labels;
    }

    private static int[][] InitializeCentroids(int[][] data, int k)
    {
        int n = data.Length;
        int m = data[0].Length;

        int[][] centroids = new int[k][];

        // Select the first centroid randomly
        centroids[0] = data[random.Next(n)];

        // Calculate distances to the first centroid
        double[] distances = new double[n];
        for (int i = 0; i < n; i++)
            distances[i] = EuclideanDistance(data[i], centroids[0]);

        // Select remaining centroids
        for (int i = 1; i < k; i++)
        {
            // Select the next centroid with probability proportional to distance
            double sum = distances.Sum();
            double r = random.NextDouble() * sum;
            double partialSum = 0;

            for (int j = 0; j < n; j++)
            {
                partialSum += distances[j];
                if (partialSum >= r)
                {
                    centroids[i] = data[j];
                    break;
                }
            }

            // Update distances using the new centroid
            for (int j = 0; j < n; j++)
            {
                double distance = EuclideanDistance(data[j], centroids[i]);
                distances[j] = Math.Min(distance, distances[j]);
            }
        }

        return centroids;
    }

    private static double GetMinimumDistance(int[] point, int[][] centroids)
    {
        double minDistance = double.MaxValue;
        foreach (var centroid in centroids)
        {
            double distance = EuclideanDistance(point, centroid);
            if (distance < minDistance)
                minDistance = distance;
        }
        return minDistance;
    }

    private static int FindClosestCentroid(int[] value, int[][] centroids)
    {
        double minDistance = double.MaxValue;
        int label = 0;
        for (int i = 0; i < centroids.Length; i++)
        {
            double distance = EuclideanDistance(value, centroids[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                label = i;
            }
        }
        return label;
    }

    private static double EuclideanDistance(int[] point1, int[] point2)
    {
        double sum = 0;
        for (int i = 0; i < point1.Length; i++)
        {
            double diff = point1[i] - point2[i];
            sum += diff * diff;
        }
        return sum; // No square root for speed optimization
    }

    private static int[][] UpdateCentroids(int[][] data, int[] labels, int k)
    {
        int[][] newCentroids = new int[k][];
        int[] counts = new int[k];
        for (int i = 0; i < k; i++)
            newCentroids[i] = new int[data[0].Length];

        for (int i = 0; i < data.Length; i++)
        {
            for (int j = 0; j < data[i].Length; j++)
                newCentroids[labels[i]][j] += data[i][j];
            counts[labels[i]]++;
        }

        for (int i = 0; i < k; i++)
            if (counts[i] > 0)
                for (int j = 0; j < newCentroids[i].Length; j++)
                    newCentroids[i][j] /= counts[i];

        return newCentroids;
    }
}