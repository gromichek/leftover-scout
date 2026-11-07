using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeftoverScout.Core.Formatting;
using LeftoverScout.Core.Help;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// The Scan page: start and cancel a scan, show progress and the summary of the last completed scan.
/// The scan itself runs off the UI thread in <see cref="IScanService"/>.
/// </summary>
public sealed partial class ScanPageViewModel : PageViewModel
{
    private const int CurrentFolderMaxLength = 80;

    private readonly IScanService _scanService;
    private CancellationTokenSource? _cancellation;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelScanCommand))]
    private bool _isScanning;

    [ObservableProperty]
    private string _currentFolderText = string.Empty;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private string _resultMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSummary))]
    private IReadOnlyList<SummaryItem> _summaryItems = [];

    [ObservableProperty]
    private IReadOnlyList<SummaryItem> _largestFolders = [];

    /// <summary>Creates the Scan page.</summary>
    public ScanPageViewModel(IScanService scanService)
        : base("Scan", HelpTopics.Scan)
    {
        _scanService = scanService;
    }

    /// <summary>One-sentence explanation shown above the buttons.</summary>
    public string Description => "Measures what takes space in your user profile. Scanning is read-only: nothing is changed or deleted.";

    /// <summary>True when a summary of a completed scan is shown.</summary>
    public bool HasSummary => SummaryItems.Count > 0;

    private bool CanStartScan() => !IsScanning;

    private bool CanCancelScan() => IsScanning;

    [RelayCommand(CanExecute = nameof(CanStartScan))]
    private async Task StartScanAsync()
    {
        IsScanning = true;
        ResultMessage = string.Empty;
        SummaryItems = [];
        LargestFolders = [];
        CurrentFolderText = string.Empty;
        ProgressText = string.Empty;
        StatusMessage = "Scanning…";

        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        var stopwatch = Stopwatch.StartNew();

        // Created on the UI thread, so reports arrive on the UI thread.
        var progress = new Progress<ScanProgress>(report =>
        {
            if (!IsScanning)
            {
                return;
            }

            CurrentFolderText = PathShortener.Shorten(report.CurrentFolder, CurrentFolderMaxLength);
            ProgressText =
                $"{report.FoldersScanned:N0} folders, {report.FilesScanned:N0} files, " +
                $"{SizeFormatter.Format(report.LocalBytes)} so far, {DurationFormatter.Format(stopwatch.Elapsed)} elapsed";
        });

        try
        {
            ScanResult result = await _scanService.RunAsync(progress, cancellation.Token);
            if (result.IsCompleted)
            {
                ShowSummary(ScanSummary.From(result));
                StatusMessage = $"Scan completed in {DurationFormatter.Format(result.Duration)}";
            }
            else
            {
                ResultMessage = "Scan cancelled.";
                StatusMessage = "Scan cancelled";
            }
        }
        catch (Exception ex)
        {
            ResultMessage = $"The scan failed: {ex.Message}";
            StatusMessage = "Scan failed";
        }
        finally
        {
            _cancellation = null;
            CurrentFolderText = string.Empty;
            ProgressText = string.Empty;
            IsScanning = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelScan))]
    private void CancelScan() => _cancellation?.Cancel();

    private void ShowSummary(ScanSummary summary)
    {
        SummaryItems =
        [
            new("Local size", SizeFormatter.Format(summary.LocalSize)),
            new("Cloud-only size (not downloaded)", SizeFormatter.Format(summary.CloudOnlySize)),
            new("Personal folders", SizeFormatter.Format(summary.PersonalSize)),
            new("Folders", summary.FolderCount.ToString("N0")),
            new("Files", summary.FileCount.ToString("N0")),
            new("Skipped links", summary.SkippedLinkCount.ToString("N0")),
            new("Cloud folders not scanned", summary.CloudFoldersNotScannedCount.ToString("N0")),
            new("Inaccessible locations", summary.InaccessibleCount.ToString("N0")),
            new("Duration", DurationFormatter.Format(summary.Duration)),
        ];

        LargestFolders = summary.LargestTopLevelFolders
            .Select(folder => new SummaryItem(folder.Name, SizeFormatter.Format(folder.LocalSize)))
            .ToList();
    }
}
