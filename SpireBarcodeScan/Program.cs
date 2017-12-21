using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SpireBarcodeScan
{
    internal static class Program
    {
        private static bool KeepConsoleOpen => AppSettings.KeepConsoleOpen;

        private static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                //to split a pdf - will keep original and create new pdf with defined page range
                if (args[0] == "split")
                {
                    var filename = args[1];
                    var startpage = int.Parse(args[2]);
                    var endpage = int.Parse(args[3]);

                    PdfSplitter.SplitPdf(filename, startpage, endpage);
                }

                //just to scan an image file and see what barcodes are detected
                if (args[0] == "scanimage")
                {
                    var image = args[1];
                    var detectedBarcodes = ScanImageWithSpire(image);
                    Console.WriteLine(JsonConvert.SerializeObject(detectedBarcodes));
                }
            }
            else
            {
                var startTime = DateTime.Now;
                Console.WriteLine($"Started at {startTime:yyyy-MM-dd HH:mm:ss}");
                DocumentProcessing.StartProcess();
                var endTime = DateTime.Now;
                Console.WriteLine($"Ended at {endTime:yyyy-MM-dd HH:mm:ss}");
                if (!KeepConsoleOpen) return;
                Console.WriteLine($"Task took {endTime.Subtract(startTime).TotalSeconds} seconds to run");
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static IEnumerable<string> ScanImageWithSpire(string image)
        {
            var scanningResult = Spire.Barcode.BarcodeScanner.Scan(image);
            return scanningResult.Distinct();

        }
    }
}
