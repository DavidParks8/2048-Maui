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

        RebuildContentContainer();
    }

    private void OnHeaderChanged(string? oldValue, string? newValue)
    {
        HeaderLabel.Text = newValue ?? string.Empty;
        HeaderLabel.IsVisible = !string.IsNullOrWhiteSpace(newValue);
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildContentContainer();
    }

    private void RebuildContentContainer()
    {
        ContentContainer.Children.Clear();

        // Treat the Items collection as the "logical" items; ignore any explicitly-added separators
        // to avoid double separators.
        List<View> logicalItems = new(Items.Count);
        foreach (var item in Items)
        {
            if (item is not NativeListSeparator)
            {
                logicalItems.Add(item);
            }
        }

        for (int i = 0; i < logicalItems.Count; i++)
        {
            ContentContainer.Children.Add(logicalItems[i]);

            if (i < logicalItems.Count - 1)
            {
                ContentContainer.Children.Add(new NativeListSeparator());
            }
        }
    }
}
