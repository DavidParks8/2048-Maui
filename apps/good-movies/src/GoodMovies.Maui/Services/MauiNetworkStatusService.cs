using GoodMovies.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Networking;

namespace GoodMovies.Maui.Services;

public sealed class MauiNetworkStatusService : INetworkStatusService, IDisposable
{
    public MauiNetworkStatusService()
    {
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    public bool IsInternetAvailable => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler? NetworkStatusChanged;

    public void Dispose()
    {
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (MainThread.IsMainThread)
        {
            NetworkStatusChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
            NetworkStatusChanged?.Invoke(this, EventArgs.Empty)
        );
    }
}
