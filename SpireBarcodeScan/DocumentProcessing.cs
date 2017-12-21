using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SpireBarcodeScan
{
    public static class DocumentProcessing
    {
        private static string ScanFolder => AppSettings.ScanFolder;  //folder that contains the files to be scanned
        private static string InProgress => AppSettings.StorageFolder; //folder that we move the files out of scanned folder into
        private static string ProcessedFolder => AppSettings.ProcessedFolder; //folder that we save the new pdfs into        
        private static string ArchiveFolder => AppSettings.ArchiveFolder; //folder that we move orginal files to once processed
        private static bool RecheckInProgressFolder => AppSettings.RecheckInProgressFolder; //so that we can add files into inprogress manually and they will get processed
        private static string Environment => AppSettings.Environment;
        private static readonly int NumberOfFilesToFetch = AppSettings.NumberOfFilesToFetch;

        public static void StartProcess()
        {
            //1. get files to work with
            FindAndMoveFiles(false, "*.pdf", NumberOfFilesToFetch);

            //2. process these files to find the barcodes
            ProcessFiles();            
        }

        private static void FindAndMoveFiles(bool searchSubFolders, string extension, int numberToFetch)
        {
            var files = DirectoryHelper.GetFiles(ScanFolder, searchSubFolders, extension, numberToFetch);
            if (!files.Any())
            {
                Console.WriteLine("There are 0 files to move");
                return;
            }

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var newFile = $@"{InProgress}\{fileName}";

                //move the file to the InProgress folder
                DirectoryHelper.MoveFile(file, newFile);
            }
        }

        private static void ProcessFiles()
        {
            while (true)
            {
                //work on the files in the InProgress folder
                Console.WriteLine("Processing files...");

                var files = DirectoryHelper.GetFiles(InProgress, false, "*.pdf", NumberOfFilesToFetch);
                if (files.Length == 0) break;
                foreach (var file in files.ToList())
                {
                    try
                    {
                        ScanningProcess.ProcessPdfFile(file, ProcessedFolder, ArchiveFolder);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error has occured processing {file}. The error is {ex.Message}");
                    }
                }

                if (RecheckInProgressFolder)
                {
                    continue;
                }
                break;
            }
        }

        public static void UpdateDatabaseManager(IEnumerable<BarcodeDocument> barcodeDocuments)
        {
            string connection;
            switch (Environment.ToLower())
            {
                case "prod":
                    connection = "DatabaseEntitiesProd";
                    break;
                case "dev":
                    connection = "DatabaseEntitiesDev";
                    break;
                case "test":
                    connection = "DatabaseEntitiesTest";
                    break;
                default:
                    connection = "DatabaseEntities";
                    break;
            }

            //send barcodes to the WFM table             
            using (var db = new DatabaseEntities(connection))
            {
                //get relevant barcodes ie those that are the eurofins sample numbers
                var relBarcodes = barcodeDocuments.Where(x=>x.IsSampleBarcode).ToList();
                if (!relBarcodes.Any()) return;
                    
                //var fileName = relBarcodes.First().FileName;               

                var processedLocation = relBarcodes.First().ProcessedFilePath;                

                var fullPath = new Uri(processedLocation, UriKind.Absolute);
                var relRoot = new Uri($@"{ProcessedFolder}", UriKind.Absolute);
                var relPath = relRoot.MakeRelativeUri(fullPath).ToString();

                foreach (var barcode in relBarcodes)
                {                                          
                    try
                    {
                        if (barcode.Barcode.StartsWith("03"))
                            barcode.Barcode = $"0{barcode.Barcode}";
                        if (barcode.Barcode.StartsWith("400"))
                            barcode.Barcode = $"003{barcode.Barcode}";

                        var barcodeRecord = new SampleSubmissionBarcodeDocument
                        {
                            Barcode = barcode.Barcode,
                            ProcessedOnDate = DateTime.UtcNow,
                            StoredLocation = relPath
                        };

                        db.SampleSubmissionBarcodeDocuments.Add(barcodeRecord);
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Could not save to database");
                    }
                }                                                              
            }
        }
        
        public static bool IsSampleBarcode(string barcode)
        {
            return (barcode.StartsWith("003") && barcode.Length==18) || 
                   (barcode.StartsWith("03") && barcode.Length == 17) || 
                   (barcode.StartsWith("400") && barcode.Length==15);
        }
    }
}
