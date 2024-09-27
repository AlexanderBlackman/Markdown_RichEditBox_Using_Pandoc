using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pandoc;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
//using Windows.Storage.Streams;

namespace Rtf2MdRichEditBox;

public sealed class MarkdownRichEditBox : RichEditBox
{
    private PandocEngine? pandoc;

    public MarkdownRichEditBox()
    {
        this.DefaultStyleKey = typeof(MarkdownRichEditBox);
        this.Loaded += MarkdownRichEditBox_Loaded;
       this.LostFocus += MarkdownRichEditBox_LostFocus;
    }

    public static readonly DependencyProperty MarkdownTextProperty =
        DependencyProperty.Register(nameof(MarkdownText), typeof(string), typeof(MarkdownRichEditBox),
            new PropertyMetadata(string.Empty, OnMarkdownTextChanged));

    public string MarkdownText
    {
        get { return (string)GetValue(MarkdownTextProperty); }
        set { SetValue(MarkdownTextProperty, value); }
    }

    private static void OnMarkdownTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownRichEditBox control)
        {
            _ = control.ConvertMarkdownToRtf((string)e.NewValue);
        }
    }

    private async Task ConvertRtfToMarkdown(string rtfText)
    {
        try
        {
            if (pandoc == null) return;
            var markdownText = await pandoc.ConvertToText<RtfIn, PandocMdOut>(rtfText);
            MarkdownText = markdownText.Value;
            Debug.WriteLine(MarkdownText);
        }
        catch (Exception ex)
        {
            // Handle or log the exception
            System.Diagnostics.Debug.WriteLine($"Error converting RTF to Markdown: {ex.Message}");
        }
    }

    private async Task ConvertMarkdownToRtf(string? markdownText = null)
    {
        try
        {
            if (pandoc == null) return;
            if (string.IsNullOrEmpty(markdownText))
            {
                markdownText = string.Empty;
            }


            var rtf = await pandoc.ConvertToText<PandocMdIn, RtfOut>(markdownText);
            var rtfText = rtf.Value;

            //Debug.WriteLine($"Raw RTF: {rtfText}");
            if (!rtfText.StartsWith(@"{\rtf1"))
                rtfText = @"{\rtf1\ansi\deff0 " + rtfText + "}";
            //Debug.WriteLine($"Final RTF: {rtfText}");

            this.Document.SetText(TextSetOptions.FormatRtf, rtfText);

            // Ensure the text is visible
            this.Document.Selection.StartPosition = 0;
            this.Document.Selection.EndPosition = 0;
        }
        catch (Exception ex)
        {
            // Handle or log the exception
            System.Diagnostics.Debug.WriteLine($"Error converting Markdown to RTF: {ex.Message}");
        }
    }

    private async void MarkdownRichEditBox_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            pandoc = new PandocEngine(@"C:\Users\Hephaestus\AppData\Local\Pandoc\pandoc.exe");
            await ConvertMarkdownToRtf(MarkdownText);
        }
        catch (Exception ex)
        {
            // Handle or log the exception
            System.Diagnostics.Debug.WriteLine($"Error initializing Pandoc: {ex.Message}");
        }
    }

    private async void MarkdownRichEditBox_LostFocus(object sender, RoutedEventArgs e)
    {
        this.Document.GetText(TextGetOptions.FormatRtf, out string rtfText);
        await ConvertRtfToMarkdown(rtfText);
    }
}