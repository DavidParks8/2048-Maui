using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GoodMovies.ViewModels;

public sealed partial class NavigationViewModel : ObservableObject, IDisposable
{
    private readonly CatalogViewModel _catalogViewModel;
    private bool _disposed;

    public NavigationViewModel(CatalogViewModel catalogViewModel)
    {
        _catalogViewModel =
            catalogViewModel ?? throw new ArgumentNullException(nameof(catalogViewModel));
        _catalogViewModel.PropertyChanged += OnCatalogPropertyChanged;
    }

    public CatalogSection SelectedSection => _catalogViewModel.SelectedSection;

    public int ComingSoonCount => _catalogViewModel.ComingSoonCount;

    public int FavoriteCount => _catalogViewModel.FavoriteCount;

    [RelayCommand]
    private void SwitchSection(CatalogSection section) => _catalogViewModel.SwitchSection(section);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _catalogViewModel.PropertyChanged -= OnCatalogPropertyChanged;
    }

    private void OnCatalogPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CatalogViewModel.SelectedSection):
                OnPropertyChanged(nameof(SelectedSection));
                break;
            case nameof(CatalogViewModel.ComingSoonCount):
                OnPropertyChanged(nameof(ComingSoonCount));
                break;
            case nameof(CatalogViewModel.FavoriteCount):
                OnPropertyChanged(nameof(FavoriteCount));
                break;
        }
    }
}
