using TwentyFortyEight.Maui.Resources.Strings;

namespace TwentyFortyEight.Maui;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
    }

    private async void OnGitHubIssuesClicked(object sender, EventArgs e)
    {
        try
        {
            await Launcher.OpenAsync("https://github.com/DavidParks8/2048-Maui/issues");
        }
        catch (Exception ex)
        {
            // If opening the URL fails, show an alert
            await DisplayAlertAsync(
                AppStrings.ErrorTitle,
                string.Format(AppStrings.UnableToOpenGitHubIssues, ex.Message),
                AppStrings.OK
            );
        }
    }
}
