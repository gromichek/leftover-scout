using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using LeftoverScout.Core.Help;
using LeftoverScout.Core.Settings;
using LeftoverScout.Core.Shell;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// The Help page: topic list, language switch (EN / RU) and the Markdown text of the selected topic.
/// The view turns <see cref="Markdown"/> into a document.
/// </summary>
public sealed partial class HelpPageViewModel : PageViewModel
{
    private readonly HelpCatalog _catalog;
    private readonly ISettingsStore _settingsStore;
    private readonly IShellLauncher _shell;

    [ObservableProperty]
    private HelpTopic? _selectedTopic;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEnglish))]
    [NotifyPropertyChangedFor(nameof(IsRussian))]
    private HelpLanguage _language;

    [ObservableProperty]
    private string _markdown = string.Empty;

    /// <summary>Creates the Help page and picks the language from the saved setting or the Windows UI language (D-028).</summary>
    /// <param name="catalog">Source of the help texts.</param>
    /// <param name="settingsStore">Remembers the chosen language.</param>
    /// <param name="shell">Opens <c>https://</c> links in the browser.</param>
    public HelpPageViewModel(HelpCatalog catalog, ISettingsStore settingsStore, IShellLauncher shell)
        : this(catalog, settingsStore, shell, CultureInfo.CurrentUICulture)
    {
    }

    /// <summary>Creates the Help page for a given Windows UI culture (used by tests).</summary>
    public HelpPageViewModel(HelpCatalog catalog, ISettingsStore settingsStore, IShellLauncher shell, CultureInfo uiCulture)
        : base("Help", HelpTopics.Help)
    {
        _catalog = catalog;
        _settingsStore = settingsStore;
        _shell = shell;

        _language = HelpLanguages.Resolve(settingsStore.Load().HelpLanguage, uiCulture);
        ShowTopic(HelpTopics.Overview);
    }

    /// <summary>All topics in display order.</summary>
    public IReadOnlyList<HelpTopic> Topics => HelpTopics.All;

    /// <summary>True when the help is shown in English. Setting it to true switches the language and saves the choice.</summary>
    public bool IsEnglish
    {
        get => Language == HelpLanguage.English;
        set
        {
            if (value)
            {
                SetLanguage(HelpLanguage.English);
            }
        }
    }

    /// <summary>True when the help is shown in Russian. Setting it to true switches the language and saves the choice.</summary>
    public bool IsRussian
    {
        get => Language == HelpLanguage.Russian;
        set
        {
            if (value)
            {
                SetLanguage(HelpLanguage.Russian);
            }
        }
    }

    /// <summary>Selects a topic by id. An unknown id selects the overview.</summary>
    public void ShowTopic(string topicId)
    {
        SelectedTopic = HelpTopics.FindById(topicId) ?? HelpTopics.All[0];
        Markdown = _catalog.GetMarkdown(SelectedTopic.Id, Language);
    }

    /// <summary>Handles a click on a link of the current topic: another topic opens here, <c>https://</c> opens the browser.</summary>
    public void OpenLink(string href)
    {
        HelpLink link = HelpLink.Resolve(href);
        switch (link.Kind)
        {
            case HelpLinkKind.Topic:
                ShowTopic(link.Target);
                break;
            case HelpLinkKind.External:
                _shell.OpenUrl(link.Target);
                break;
        }
    }

    partial void OnSelectedTopicChanged(HelpTopic? value)
    {
        if (value is not null)
        {
            Markdown = _catalog.GetMarkdown(value.Id, Language);
        }
    }

    private void SetLanguage(HelpLanguage language)
    {
        if (Language == language)
        {
            return;
        }

        Language = language;
        _settingsStore.Save(_settingsStore.Load() with { HelpLanguage = HelpLanguages.ToCode(language) });
        if (SelectedTopic is not null)
        {
            Markdown = _catalog.GetMarkdown(SelectedTopic.Id, language);
        }
    }
}
