// 1. create maui app with name "tempmaui"
// 2. delete: App.xaml, AppShell.xaml, MainPage.xaml, MauiProgram.cs
// 3. create new class and name it "tempmaui"
// 4. use LLMs (e.g. Copilot) to learn more about .NET MAUI

namespace tempmaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp() =>
        MauiApp.CreateBuilder().UseMauiApp<App>()
        .Build();
}

public class App : Application
{
    public App()
    {
        MainPage = new ContentPage
        {
            Content = new Label
            {
                Text = "Hello, World!",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 24,
            }
        };
    }
}

/*
public static class MauiProgram
{
    public static MauiApp CreateMauiApp() => 
        MauiApp.CreateBuilder().UseMauiApp<App>()
        .Build();
}

public class App : Application
{
    public App() => MainPage = new MainPage();
}

public class MainPage : ContentPage
{
    public MainPage()
    {
        BackgroundColor = Colors.White;
        Content = new Label
        {
            Text = "Hello .NET MAUI",
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            FontSize = 24,
            TextColor = Colors.Black
        };
    }
}
*/
