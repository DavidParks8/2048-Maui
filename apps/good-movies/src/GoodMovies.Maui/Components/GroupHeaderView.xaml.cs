using GoodMovies.Maui.Converters;
using GoodMovies.Maui.Resources.Strings;
using GoodMovies.ViewModels;
using Maui.BindableProperty.Generator.Core;

namespace GoodMovies.Maui.Components;

public partial class GroupHeaderView : ContentView
{
#pragma warning disable CS0169
    [AutoBindable(OnChanged = nameof(OnGroupChanged))]
    private readonly MovieGroupViewModel? _group;
#pragma warning restore CS0169

    public GroupHeaderView()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
    }

    private void OnGroupChanged(MovieGroupViewModel? oldGroup, MovieGroupViewModel? newGroup) =>
        UpdateTitle();

    private void OnSizeChanged(object? sender, EventArgs e) => UpdateTitle();

    private void UpdateTitle()
    {
        TitleLabel.Text = Group switch
        {
            { IsInTheatersNow: true } => AppStrings.InTheatersNow,
            { ReleaseDate: DateOnly date } when Width > 0 && Width < 500 =>
                GoodMoviesTextFormatter.FormatCompactDate(date),
            { ReleaseDate: DateOnly date } => GoodMoviesTextFormatter.FormatDate(date),
            _ => AppStrings.ComingSoon,
        };
    }
}
