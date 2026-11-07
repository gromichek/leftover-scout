using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Markdig;
using MdInlines = Markdig.Syntax.Inlines;
using MdSyntax = Markdig.Syntax;
using MdTables = Markdig.Extensions.Tables;

namespace LeftoverScout.App.Help;

/// <summary>
/// Turns the allowed Markdown subset (headings, paragraphs, bold, italic, inline code, lists, pipe tables, block quotes,
/// links) into a WPF <see cref="FlowDocument"/>. Anything else is shown as plain text and never throws.
/// WPF objects must be created on an STA thread (the UI thread). Sources: Markdig AST (markdig docs, "Parsing"),
/// Microsoft Learn "FlowDocument Overview" and "Flow Content Elements".
/// </summary>
public sealed class HelpDocumentBuilder
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UsePipeTables().Build();

    private readonly Action<string> _linkClicked;
    private readonly Brush _textBrush;
    private readonly Brush _mutedBrush;
    private readonly Brush _codeBackground;
    private readonly Brush _lineBrush;

    /// <summary>Creates a builder.</summary>
    /// <param name="linkClicked">Called with the link address (as written in the Markdown) when a link is clicked.</param>
    public HelpDocumentBuilder(Action<string> linkClicked)
    {
        _linkClicked = linkClicked;

        // Colors come from the Fluent light theme when it is loaded; fixed fallbacks keep the builder usable without an Application.
        _textBrush = FindBrush("TextFillColorPrimaryBrush") ?? Brushes.Black;
        _mutedBrush = FindBrush("TextFillColorSecondaryBrush") ?? Brushes.DimGray;
        _codeBackground = FindBrush("ControlFillColorSecondaryBrush") ?? new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
        _lineBrush = FindBrush("ControlStrokeColorDefaultBrush") ?? Brushes.LightGray;
    }

    /// <summary>Builds the document for a Markdown text.</summary>
    public FlowDocument Build(string markdown)
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14,
            Foreground = _textBrush,
            PagePadding = new Thickness(4, 0, 16, 16),
            MaxPageWidth = 760,
            TextAlignment = TextAlignment.Left,
        };

        try
        {
            MdSyntax.MarkdownDocument parsed = Markdown.Parse(markdown, Pipeline);
            AddBlocks(document.Blocks, parsed);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Never crash the app because of a help file: show the raw text instead.
            document.Blocks.Clear();
            document.Blocks.Add(new Paragraph(new Run(markdown)));
        }

        return document;
    }

    private static Brush? FindBrush(string key) => Application.Current?.TryFindResource(key) as Brush;

    private void AddBlocks(BlockCollection target, MdSyntax.ContainerBlock container)
    {
        foreach (MdSyntax.Block block in container)
        {
            switch (block)
            {
                case MdSyntax.HeadingBlock heading:
                    target.Add(CreateHeading(heading));
                    break;
                case MdSyntax.ParagraphBlock paragraph:
                    target.Add(CreateParagraph(paragraph.Inline, new Thickness(0, 0, 0, 10)));
                    break;
                case MdSyntax.ListBlock list:
                    target.Add(CreateList(list));
                    break;
                case MdSyntax.QuoteBlock quote:
                    target.Add(CreateQuote(quote));
                    break;
                case MdTables.Table table:
                    target.Add(CreateTable(table));
                    break;
                case MdSyntax.ThematicBreakBlock:
                    target.Add(new Paragraph { Margin = new Thickness(0, 4, 0, 10), BorderBrush = _lineBrush, BorderThickness = new Thickness(0, 1, 0, 0) });
                    break;
                case MdSyntax.CodeBlock code:
                    target.Add(CreatePlainBlock(code.Lines.ToString(), monospace: true));
                    break;
                case MdSyntax.LeafBlock leaf:
                    target.Add(CreatePlainBlock(leaf.Lines.ToString(), monospace: false));
                    break;
                case MdSyntax.ContainerBlock inner:
                    AddBlocks(target, inner);
                    break;
            }
        }
    }

    private Paragraph CreateHeading(MdSyntax.HeadingBlock heading)
    {
        Paragraph paragraph = CreateParagraph(heading.Inline, heading.Level switch
        {
            1 => new Thickness(0, 0, 0, 12),
            2 => new Thickness(0, 16, 0, 8),
            _ => new Thickness(0, 12, 0, 6),
        });
        paragraph.FontWeight = FontWeights.SemiBold;
        paragraph.FontSize = heading.Level switch
        {
            1 => 26,
            2 => 20,
            _ => 16,
        };
        return paragraph;
    }

    private Paragraph CreateParagraph(MdInlines.ContainerInline? inline, Thickness margin)
    {
        var paragraph = new Paragraph { Margin = margin, LineHeight = 21 };
        AddInlines(paragraph.Inlines, inline);
        return paragraph;
    }

    private Paragraph CreatePlainBlock(string text, bool monospace)
    {
        var paragraph = new Paragraph(new Run(text)) { Margin = new Thickness(0, 0, 0, 10) };
        if (monospace)
        {
            paragraph.FontFamily = new FontFamily("Consolas");
            paragraph.Background = _codeBackground;
            paragraph.Padding = new Thickness(8, 4, 8, 4);
        }

        return paragraph;
    }

    private List CreateList(MdSyntax.ListBlock list)
    {
        var result = new List
        {
            MarkerStyle = list.IsOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(24, 0, 0, 0),
        };

        foreach (MdSyntax.Block itemBlock in list)
        {
            if (itemBlock is not MdSyntax.ListItemBlock item)
            {
                continue;
            }

            var listItem = new ListItem();
            AddBlocks(listItem.Blocks, item);
            foreach (Block child in listItem.Blocks)
            {
                if (child is Paragraph paragraph)
                {
                    paragraph.Margin = new Thickness(0, 0, 0, 4);
                }
            }

            result.ListItems.Add(listItem);
        }

        return result;
    }

    private Section CreateQuote(MdSyntax.QuoteBlock quote)
    {
        var section = new Section
        {
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(12, 2, 0, 2),
            BorderBrush = _lineBrush,
            BorderThickness = new Thickness(3, 0, 0, 0),
            Foreground = _mutedBrush,
        };
        AddBlocks(section.Blocks, quote);
        return section;
    }

    private Table CreateTable(MdTables.Table markdownTable)
    {
        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, 0, 0, 12),
            BorderBrush = _lineBrush,
            BorderThickness = new Thickness(1),
        };

        int columns = Math.Max(1, markdownTable.ColumnDefinitions.Count);
        foreach (MdSyntax.Block rowBlock in markdownTable)
        {
            if (rowBlock is MdTables.TableRow row)
            {
                columns = Math.Max(columns, row.Count);
            }
        }

        for (int i = 0; i < columns; i++)
        {
            table.Columns.Add(new TableColumn());
        }

        var group = new TableRowGroup();
        foreach (MdSyntax.Block rowBlock in markdownTable)
        {
            if (rowBlock is not MdTables.TableRow row)
            {
                continue;
            }

            var tableRow = new TableRow();
            if (row.IsHeader)
            {
                tableRow.Background = _codeBackground;
                tableRow.FontWeight = FontWeights.SemiBold;
            }

            foreach (MdSyntax.Block cellBlock in row)
            {
                if (cellBlock is not MdTables.TableCell cell)
                {
                    continue;
                }

                var tableCell = new TableCell
                {
                    Padding = new Thickness(8, 4, 8, 4),
                    BorderBrush = _lineBrush,
                    BorderThickness = new Thickness(0, 0, 1, 1),
                };
                AddBlocks(tableCell.Blocks, cell);
                foreach (Block child in tableCell.Blocks)
                {
                    child.Margin = new Thickness(0);
                }

                tableRow.Cells.Add(tableCell);
            }

            group.Rows.Add(tableRow);
        }

        table.RowGroups.Add(group);
        return table;
    }

    private void AddInlines(InlineCollection target, MdInlines.ContainerInline? container)
    {
        if (container is null)
        {
            return;
        }

        foreach (MdInlines.Inline inline in container)
        {
            switch (inline)
            {
                case MdInlines.LiteralInline literal:
                    target.Add(new Run(literal.Content.ToString()));
                    break;
                case MdInlines.EmphasisInline emphasis:
                    Span span = emphasis.DelimiterCount >= 2 ? new Bold() : new Italic();
                    AddInlines(span.Inlines, emphasis);
                    target.Add(span);
                    break;
                case MdInlines.CodeInline code:
                    target.Add(new Run(code.Content)
                    {
                        FontFamily = new FontFamily("Consolas"),
                        Background = _codeBackground,
                    });
                    break;
                case MdInlines.LineBreakInline lineBreak:
                    target.Add(lineBreak.IsHard ? new LineBreak() : new Run(" "));
                    break;
                case MdInlines.LinkInline link when link.IsImage:
                    // Images are not allowed in help; show the alternative text.
                    AddInlines(target, link);
                    break;
                case MdInlines.LinkInline link:
                    target.Add(CreateHyperlink(link.Url ?? string.Empty, hyperlink => AddInlines(hyperlink.Inlines, link)));
                    break;
                case MdInlines.AutolinkInline autolink:
                    target.Add(CreateHyperlink(autolink.Url, hyperlink => hyperlink.Inlines.Add(new Run(autolink.Url))));
                    break;
                case MdInlines.ContainerInline inner:
                    AddInlines(target, inner);
                    break;
                default:
                    target.Add(new Run(inline.ToString() ?? string.Empty));
                    break;
            }
        }
    }

    private Hyperlink CreateHyperlink(string address, Action<Hyperlink> fillContent)
    {
        var hyperlink = new Hyperlink();
        fillContent(hyperlink);

        // No NavigateUri: relative topic links are not valid absolute URIs. Click is routed to the view model.
        hyperlink.Click += (_, _) => _linkClicked(address);
        return hyperlink;
    }
}
