using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Maui.BindableProperty.Generator.Core;

namespace TwentyFortyEight.Maui.Components;

/// <summary>
/// A specialized native-styled section component for list-based content with rows.
/// Automatically wraps children in a VerticalStackLayout with Spacing="0" and Padding="16,0".
/// Ideal for Settings-style row lists.
/// </summary>
public partial class NativeListSection : ContentView
{
#pragma warning disable CS0169 // Field is never used - used by source generator

    /// <summary>
    /// Gets or sets the section header text. Set to empty or null to hide the header.
    /// </summary>
    [AutoBindable(OnChanged = nameof(OnHeaderChanged))]
    private readonly string _header = string.Empty;

#pragma warning restore CS0169

    /// <summary>
    /// Gets the collection of child views to display in the list section.
    /// </summary>
    public ObservableCollection<View> Items { get; } = new();

    /// <summary>
    /// Gets whether the header should be visible.
    /// </summary>
    public bool HasHeader => !string.IsNullOrWhiteSpace(Header);

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeListSection"/> class.
    /// </summary>
    public NativeListSection()
    {
        InitializeComponent();
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    private void OnHeaderChanged(string? oldValue, string? newValue)
    {
        HeaderLabel.Text = newValue ?? string.Empty;
        HeaderLabel.IsVisible = !string.IsNullOrWhiteSpace(newValue);
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    int index = e.NewStartingIndex;
                    foreach (View item in e.NewItems)
                    {
                        ContentContainer.Children.Insert(index++, item);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (View item in e.OldItems)
                    {
                        ContentContainer.Children.Remove(item);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                ContentContainer.Children.Clear();
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems != null && e.NewItems != null)
                {
                    int index = e.OldStartingIndex;
                    foreach (View item in e.NewItems)
                    {
                        ContentContainer.Children[index++] = item;
                    }
                }
                break;
        }
    }
}
