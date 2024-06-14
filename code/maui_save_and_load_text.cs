namespace tempmaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp() => MauiApp.CreateBuilder().UseMauiApp<App>().Build();
}

public class App : Application
{
    public App() => MainPage = new MainPage();
}

public class MainPage : ContentPage
{
    string appDataDirectory = FileSystem.AppDataDirectory;
    ListView listView = new ListView();
    Entry entry = new() { Placeholder = "Enter file name", TextColor = Colors.White };
    Editor contentEditor = new() { HeightRequest = 200, TextColor = Colors.White };
    Button saveButton = new Button { Text = "Save File" };

    public MainPage()
    {
        // set listView text color here
        listView.ItemTemplate = new DataTemplate(() =>
        {
            var label = new Label { TextColor = Colors.White };
            label.SetBinding(Label.TextProperty, ".");
            return new ViewCell { View = label };
        });

        saveButton.Clicked += SaveButtonClicked;
        listView.ItemSelected += FileSelected;

        var label = new Label { Text = "Files:" };
        Content = new StackLayout
        {
            Children = { label, listView, entry, contentEditor, saveButton }
        };
        LoadFiles();
    }

    void SaveButtonClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(entry.Text)) return;
        var path = Path.Combine(appDataDirectory, entry.Text + ".txt");
        File.WriteAllText(path, contentEditor.Text);
        LoadFiles();
    }

    void LoadFiles()
    {
        var files = Directory.GetFiles(appDataDirectory, "*.txt");
        listView.ItemsSource = files.Select(f => Path.GetFileName(f)).ToList();
    }

    void FileSelected(object? sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem is string fileName)
        {
            var path = Path.Combine(appDataDirectory, fileName);
            if (!File.Exists(path)) return;
            var content = File.ReadAllText(path);
            entry.Text = Path.GetFileNameWithoutExtension(fileName);
            contentEditor.Text = content;
        }
    }
}