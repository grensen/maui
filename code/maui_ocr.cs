// .NET MAUI NO-XAML MARKUP

// https://github.com/kfrancis/ocr
// https://youtu.be/alY_6Qn0_60
// https://github.com/jfversluis/MauiOcrPluginSample

// Add Android permissions (Platforms/Android/Resources/AndroidManifest.xml)
// <uses-permission android:name="android.permission.CAMERA"/>
// <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE"/>

// install NuGet packages: 
// 1. CommunityToolkit.Maui.Markup
// 2. Plugin.Maui.OCR 

using CommunityToolkit.Maui.Markup;
using Plugin.Maui.OCR;

namespace tempmaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp() =>
        MauiApp.CreateBuilder().UseMauiApp<App>()
        .UseMauiCommunityToolkitMarkup()
        .UseOcr()
        .Build();
}

public class App : Application
{
    public App() => MainPage = new MainPage();  
}

public class MainPage : ContentPage
{
    bool tryHard = !false;
    public MainPage()
    {
        var vStack = new VerticalStackLayout
        {
            Spacing = 25,
            Children =
            {
                new Label { Text = "MAUI OCR Demo" }
                .Font(size: 32)
                .CenterHorizontal()
                                
                ,CreateButton($"Try Hard {tryHard}")
                .Assign(out Button countLabel)
                .Invoke(b => b.Clicked += (sender, e) =>
                {
                    tryHard = !tryHard;
                    countLabel.Text = $"Try Hard {tryHard}";

                }),

                CreateButton("Pick Image")
                .Invoke(b => b.Clicked += async (sender, e) =>
                {
                    var pick = await MediaPicker.Default.PickPhotoAsync();
                    await OCR(pick, tryHard);
                }),

                 CreateButton("Take Picture")
                .Invoke(b => b.Clicked += async (sender, e) =>
                {
                    FileResult? capture = await MediaPicker.Default.CapturePhotoAsync();
                    await OCR(capture, tryHard);
                })                                 
            }
        }.Paddings(30, 30, 30, 30);
        Content = new ScrollView { Content = vStack };
    }

    async Task OCR(FileResult? fileResult, bool isHard)
    {
        if (fileResult != null)
        {
            using var imageAsStream = await fileResult.OpenReadAsync();
            var imageAsBytes = new byte[imageAsStream.Length];
            await imageAsStream.ReadAsync(imageAsBytes, 0, (int)imageAsStream.Length);

            var ocrResult = await OcrPlugin.Default.RecognizeTextAsync(imageAsBytes, isHard);

            if (!ocrResult.Success)
            {
                await DisplayAlert("No success", "No OCR possible", "OK");
                return;
            }

            await DisplayAlert("OCR Result", ocrResult.AllText, "OK");
        }
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await OcrPlugin.Default.InitAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"OCR Initialization failed: {ex.Message}", "OK");
        }
    }

    static Button CreateButton(string s) => new Button
    {
        Text = s
    }
    .Font(bold: true)
        .CenterHorizontal();
}