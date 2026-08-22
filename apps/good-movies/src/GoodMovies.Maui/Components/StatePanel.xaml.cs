using System.Windows.Input;
using GoodMovies.ViewModels;
using Maui.BindableProperty.Generator.Core;

namespace GoodMovies.Maui.Components;

public partial class StatePanel : ContentView
{
#pragma warning disable CS0169
    [AutoBindable]
    private readonly CatalogMessageKey _messageKey;

    [AutoBindable]
    private readonly CatalogViewState _state;

    [AutoBindable]
    private readonly ICommand? _retryCommand;
#pragma warning restore CS0169

    public StatePanel()
    {
        InitializeComponent();
    }
}
