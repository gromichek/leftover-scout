namespace LeftoverScout.App.Services;

/// <summary>Asks the user a question. An abstraction so the confirmation flow can be tested.</summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a Yes / No question with <c>No</c> as the default button and returns true only when the user answers Yes.
    /// Closing the dialog counts as No.
    /// </summary>
    bool Confirm(string title, string message);
}
