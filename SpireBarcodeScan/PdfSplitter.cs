using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace SpireBarcodeScan
{
    public static class PdfSplitter
    {
        public static void SplitPdf(string filename, int startOnPage, int endOnPage)
        {
            var reader = new PdfReader(filename);
            var nbPages = reader.NumberOfPages;
            if (endOnPage > nbPages || startOnPage > nbPages || startOnPage > endOnPage) return;

            Document newPdf = null;
            PdfCopy copy = null;

            var splitFileName = $"{filename}_s_{DateTime.UtcNow:HHmmss}.pdf";

            var fs = new FileStream(splitFileName, FileMode.Create);
            newPdf = new Document(PageSize.A4);
            copy = new PdfCopy(newPdf, fs);
            newPdf.Open();

            for (var i = startOnPage; i <= endOnPage; i++)
            {
                copy.AddPage(copy.GetImportedPage(reader, i));
            }

            newPdf.Close(); 
            copy.Close();     
            reader.Close();
        }

    }
}
