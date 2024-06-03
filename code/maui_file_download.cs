// Add Android permissions (Platforms/Android/Resources/AndroidManifest.xml)
// <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />

// .NET MAUI NO-XAML MARKUP
// install NuGet packages: 
// 1. CommunityToolkit.Maui

using CommunityToolkit.Maui;

namespace tempmaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
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
    private ProgressBar progressBar;
    private Button downloadButton, printButton;
    const string url = "https://raw.githubusercontent.com/grensen/how_to_train/main/system1.txt";
    const string netName = "system1.txt";
    readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, netName);

    public MainPage()
    {
        progressBar = new ProgressBar 
        { 
            IsVisible = false, 
            HeightRequest = 20, 
            Margin = new Thickness(0, 10) 
        };

        downloadButton = new Button 
        { 
            Text = "Download File", 
            BackgroundColor = Colors.DarkSlateBlue, 
            TextColor = Colors.White, 
            CornerRadius = 5 
        };

        printButton = new Button 
        { 
            Text = "Print File", 
            IsEnabled = false, 
            BackgroundColor = Colors.DimGray, 
            TextColor = Colors.White, 
            CornerRadius = 5 
        };
        
        downloadButton.Clicked += async (s, e) => await StartDownloadProcessAsync();
        printButton.Clicked += async (s, e) => await PrintFileContentsAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Children = {
                    new Label 
                    { 
                        Text = $"URL: {url}\n" + $"Path: {filePath}", 
                        FontSize = 12, 
                        HorizontalOptions = LayoutOptions.Center, 
                        Margin = new Thickness(0, 10) 
                    },
                    downloadButton,
                    printButton,
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

        progressBar.IsVisible = false;
        printButton.IsEnabled = true;
        printButton.BackgroundColor = Colors.DarkSlateBlue;

        async Task DownloadFileAsync()
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

    private async Task PrintFileContentsAsync()
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            await DisplayAlert("File Contents", string.Join("\n", lines.Take(1000)), "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to read file: {ex.Message}", "OK");
        }
    }
}