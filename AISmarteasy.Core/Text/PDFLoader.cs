using SemanticKernel.Memory;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace SemanticKernel.Text;

public class PdfLoader
{
    public static async Task<List<string>> SaveEmbeddingsFromDirectoryFiles(ISemanticTextMemory memory, string directory)
    {
        const int Max_Content_Item_Size = 2048;
        var memoryCollectionName = "smarteasy"; 

        var pdfFiles = Directory.GetFiles(directory, "*.pdf");
        var pageTexts = new List<string>();

        foreach (var pdfFileName in pdfFiles)
        {
            var pdfDocument = PdfDocument.Open(pdfFileName);

            foreach (var pdfPage in pdfDocument.GetPages())
            {
                var pageText = ContentOrderTextExtractor.GetText(pdfPage);
                pageTexts.Add(pageText);

                var paragraphs = new List<string>();

                if (pageText.Length > Max_Content_Item_Size)
                {
                    var lines = TextChunker.SplitPlainTextLines(pageText, Max_Content_Item_Size);
                    paragraphs = TextChunker.SplitPlainTextParagraphs(lines, Max_Content_Item_Size);
                }
                else
                {
                    paragraphs.Add(pageText);
                }

                foreach (var paragraph in paragraphs)
                {
                    var fileName = Path.GetFileName(pdfFileName);
                    var id = fileName + pdfPage.Number + paragraphs.IndexOf(paragraph);
                    var saveResult = await KernelProvider.Kernel.SaveInformationAsync(memoryCollectionName, paragraph, id).ConfigureAwait(false);
                }
            }
        }

        return pageTexts;
    }
}