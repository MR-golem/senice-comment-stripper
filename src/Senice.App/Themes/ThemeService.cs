using System.Windows;

namespace Senice.App.Themes;

public enum AppTheme
{
    Dark,
    Light,
}

public static class ThemeService
{
    public static void Apply(AppTheme theme)
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri($"/Senice.App;component/Themes/{theme}Theme.xaml", UriKind.Relative),
        };

        var resources = Application.Current.Resources;
        resources.MergedDictionaries.Clear();
        resources.MergedDictionaries.Add(dictionary);
    }
}
