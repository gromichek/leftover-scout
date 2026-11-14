using System.Windows;

namespace LeftoverScout.App.Services;

/// <summary>Shows the questions as Windows message boxes (Yes / No, default No, owned by the main window).</summary>
public sealed class WpfDialogService : IDialogService
{
    /// <inheritdoc />
    public bool Confirm(string title, string message)
    {
        Window? owner = Application.Current?.MainWindow;
        MessageBoxResult answer = owner is { IsVisible: true }
            ? MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
            : MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        return answer == MessageBoxResult.Yes;
    }
}
