using System.Windows.Documents;
using LeftoverScout.App.Help;

namespace LeftoverScout.App.Tests.Help;

public class HelpDocumentBuilderTests
{
    private static FlowDocument Build(string markdown, Action<string>? onLink = null) =>
        new HelpDocumentBuilder(onLink ?? (_ => { })).Build(markdown);

    [Fact]
    public void Build_Headings_CreateParagraphsWithDecreasingFontSize()
    {
        StaThread.Run(() =>
        {
            FlowDocument document = Build("# One\n\n## Two\n\n### Three");

            double[] sizes = document.Blocks.Cast<Paragraph>().Select(p => p.FontSize).ToArray();
            Assert.Equal(3, sizes.Length);
            Assert.True(sizes[0] > sizes[1] && sizes[1] > sizes[2]);
        });
    }

    [Fact]
    public void Build_BoldItalicAndCode_CreateSpans()
    {
        StaThread.Run(() =>
        {
            var paragraph = (Paragraph)Build("a **bold** and *italic* and `code`").Blocks.First();

            Assert.Contains(paragraph.Inlines, i => i is Bold);
            Assert.Contains(paragraph.Inlines, i => i is Italic);
            Assert.Contains(paragraph.Inlines, i => i is Run run && run.Text == "code" && run.FontFamily.Source == "Consolas");
        });
    }

    [Fact]
    public void Build_BulletAndNumberedLists_CreateLists()
    {
        StaThread.Run(() =>
        {
            FlowDocument document = Build("- one\n- two\n  - nested\n\n1. first\n2. second");

            List[] lists = document.Blocks.OfType<List>().ToArray();
            Assert.Equal(2, lists.Length);
            Assert.Equal(System.Windows.TextMarkerStyle.Disc, lists[0].MarkerStyle);
            Assert.Equal(2, lists[0].ListItems.Count);
            Assert.Contains(lists[0].ListItems.Last().Blocks, b => b is List);
            Assert.Equal(System.Windows.TextMarkerStyle.Decimal, lists[1].MarkerStyle);
            Assert.Equal(2, lists[1].ListItems.Count);
        });
    }

    [Fact]
    public void Build_PipeTable_CreatesTableWithHeaderAndRows()
    {
        StaThread.Run(() =>
        {
            FlowDocument document = Build("| A | B |\n|---|---|\n| 1 | 2 |\n| 3 | 4 |");

            Table table = Assert.IsType<Table>(Assert.Single(document.Blocks));
            Assert.Equal(2, table.Columns.Count);
            Assert.Equal(3, table.RowGroups[0].Rows.Count);
            Assert.Equal(2, table.RowGroups[0].Rows[1].Cells.Count);
        });
    }

    [Fact]
    public void Build_BlockQuote_CreatesSection()
    {
        StaThread.Run(() =>
        {
            FlowDocument document = Build("> quoted text");

            Section section = Assert.IsType<Section>(Assert.Single(document.Blocks));
            Assert.IsType<Paragraph>(Assert.Single(section.Blocks));
        });
    }

    [Fact]
    public void Build_Link_CreatesHyperlinkThatReportsItsAddress()
    {
        StaThread.Run(() =>
        {
            var clicked = new List<string>();
            var paragraph = (Paragraph)Build("see [Scan](scan.md) now", clicked.Add).Blocks.First();

            Hyperlink link = Assert.IsType<Hyperlink>(paragraph.Inlines.First(i => i is Hyperlink));
            link.RaiseEvent(new System.Windows.RoutedEventArgs(Hyperlink.ClickEvent));

            Assert.Equal(["scan.md"], clicked);
        });
    }

    [Fact]
    public void Build_ImageAndHtml_AreShownAsPlainTextWithoutThrowing()
    {
        StaThread.Run(() =>
        {
            FlowDocument document = Build("![alt text](x.png)\n\n<div>raw</div>\n\n```\ncode block\n```");

            Assert.True(document.Blocks.Count >= 2);
            Assert.DoesNotContain(document.Blocks, b => b is BlockUIContainer);
        });
    }

    [Fact]
    public void Build_EmptyText_ReturnsEmptyDocument()
    {
        StaThread.Run(() => Assert.Empty(Build(string.Empty).Blocks));
    }
}
