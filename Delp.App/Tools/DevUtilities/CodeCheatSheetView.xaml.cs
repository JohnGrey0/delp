using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("code-cheatsheet", "Programming Cheat Sheet", ToolCategory.DevUtilities,
    "Browse correct, idiomatic code snippets for common algorithms, data structures, patterns, and language constructs across eight languages.",
    Keywords = "cheatsheet,snippets,sorting,patterns,examples,learning", Order = 130)]
public partial class CodeCheatSheetView : UserControl
{
    /// <summary>Remembers the last language tab picked, so switching topics keeps it selected.</summary>
    private static string _lastLanguage = "C#";

    public CodeCheatSheetView()
    {
        InitializeComponent();
        ApplyFilter(null);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter(SearchBox.Text);

    private void ApplyFilter(string? query)
    {
        var results = CodeCheatSheetData.Search(query);

        var view = CollectionViewSource.GetDefaultView(results);
        view.GroupDescriptions.Clear();
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(CheatTopic.Category)));
        TopicList.ItemsSource = view;

        StatusText.Text = $"{results.Count} of {CodeCheatSheetData.All.Count} topics";

        if (results.Count > 0)
            TopicList.SelectedIndex = 0;
        else
            ShowTopic(null);
    }

    private void TopicList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowTopic(TopicList.SelectedItem as CheatTopic);

    private void ShowTopic(CheatTopic? topic)
    {
        if (topic is null)
        {
            TitleText.Text = "";
            ExplanationText.Text = "No topics match your search.";
            LanguageTabs.Items.Clear();
            return;
        }

        TitleText.Text = topic.Title;
        ExplanationText.Text = topic.Explanation;

        LanguageTabs.Items.Clear();
        TabItem? toSelect = null;
        foreach (var snippet in topic.Snippets)
        {
            var tab = new TabItem { Header = snippet.Language, Content = BuildSnippetPane(snippet) };
            LanguageTabs.Items.Add(tab);
            if (snippet.Language == _lastLanguage)
                toSelect = tab;
        }

        LanguageTabs.SelectedItem = toSelect ?? LanguageTabs.Items.OfType<TabItem>().FirstOrDefault();
    }

    private DockPanel BuildSnippetPane(CodeSnippet snippet)
    {
        var copyBtn = new Button { Content = "Copy", HorizontalAlignment = HorizontalAlignment.Right };
        copyBtn.Click += (_, _) => Ui.Copy(snippet.Code, copyBtn);

        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
        header.Children.Add(copyBtn);

        var editor = new TextBox
        {
            Style = (Style)FindResource("TextBox.Mono"),
            IsReadOnly = true,
            Text = snippet.Code,
        };
        var editorHost = new Border { Style = (Style)FindResource("Card.Editor"), Child = editor };

        var pane = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        pane.Children.Add(header);
        pane.Children.Add(editorHost);
        return pane;
    }

    private void LanguageTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageTabs.SelectedItem is TabItem { Header: string lang })
            _lastLanguage = lang;
    }
}
