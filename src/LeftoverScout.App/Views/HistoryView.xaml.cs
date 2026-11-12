using System.Windows.Controls;

namespace LeftoverScout.App.Views;

/// <summary>
/// Shows a <see cref="ViewModels.HistoryPageViewModel"/>. Pure view: everything is bound to the view model.
/// </summary>
public partial class HistoryView : UserControl
{
    /// <summary>Creates the view.</summary>
    public HistoryView()
    {
        InitializeComponent();
    }
}
