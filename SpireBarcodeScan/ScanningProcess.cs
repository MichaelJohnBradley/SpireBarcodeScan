using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using NLog;
using System.Drawing.Imaging;

namespace SpireBarcodeScan
{
    public static class ScanningProcess
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private const int TimeoutSeconds = 300;

        public static void ProcessPdfFile(string file, string processedLocation,string archiveLocation)
        {
            logger.Trace($"START PROCESSING: {file}");
            //storage location is the root folder where we are going to ultimately put our created pdf
            //file is the main file that is going to be split into multiple pdfs            

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            var fileNameWithExtension = Path.GetFileName(file);

            var dateFolder = DirectoryHelper.CreateFolderString("date");
            var processedSubFolder = $@"{processedLocation}\{dateFolder}";
            
            //need the processedSubFolder to exist first
            DirectoryHelper.CreateFolder(processedSubFolder);

            var reader = new PdfReader(file);
            var nbPages = reader.NumberOfPages;  //how many pages are in the pdf document
            
            Document newPdf = null;
            PdfCopy copy = null;
                        
            for (var i = 1; i <= nbPages; i++)
            {
                logger.Trace($"PROCESSING PAGE: {i} of {nbPages}");
                //1. turn each page into image(s)
                var imageBytes = GetImagesFromPdfPage(reader, i);
                var timePortion = $"{DateTime.UtcNow:HHmmss}";
                var fileName = $"{fileNameWithoutExtension}-{timePortion}.pdf"; //file name of the created file
                var processedFileName = $@"{processedSubFolder}\{fileName}";  //where the created file is stored finally
                var errorFileName = $@"{archiveLocation}\Error_{fileName}";  //where the created file is stored finally
                FileStream fs;

                //for first page will create new document
                if (i == 1)
                {
                    fs = new FileStream(processedFileName, FileMode.Create);
                    newPdf = new Document(PageSize.A4);
                    copy = new PdfCopy(newPdf, fs);
                    newPdf.Open();
                    logger.Trace($"CREATE NEW PDF FILE: {processedFileName}");
                }

                //2. scan with spire.barcode to get the barcodes on the page  
                IEnumerable<string> detectedBarcodes;
                try
                {                    
                    var task = Task.Run(() => ScanImageWithSpire(imageBytes));
                    if (task.Wait(TimeSpan.FromSeconds(TimeoutSeconds)))
                    {
                        logger.Trace($"SCANNING PAGE FOR BARCODES:");
                        detectedBarcodes = task.Result;
                    }
                    else
                    {
                        logger.Trace($"Timeout Error - scanning page {i} took longer than {TimeoutSeconds} seconds");
                        throw new TimeoutException();
                    }
                }
                catch (TimeoutException ex)
                {                   
                    //add to the existing pdf
                    copy?.AddPage(copy.GetImportedPage(reader, i));

                    var error = $"Timeout Error - The following page could not be scanned for barcodes due to a timeout error: Page:{i}, ArchiveFileName: {fileNameWithExtension}, The page has been added to the following file: {fileName} and is also saved as single page pdf - {errorFileName}";
                    var errorFs = new FileStream(errorFileName, FileMode.Create);
                    var errorPageDoc = new Document(PageSize.A4);
                    var errorCopy = new PdfCopy(errorPageDoc, errorFs);
                    
                    errorPageDoc.Open();
                    errorCopy.AddPage(errorCopy.GetImportedPage(reader,i));
                    errorPageDoc.Close();
                    errorFs.Close();
                    logger.Error(error);

                    continue;                   
                }

                var barcodeDocs = new List<BarcodeDocument>();
                barcodeDocs.AddRange(detectedBarcodes.Select(result => new BarcodeDocument
                {
                    Barcode = result,
                    ProcessedFilePath = processedFileName,
                    FileName = fileName,
                    IsSampleBarcode = DocumentProcessing.IsSampleBarcode(result)
                }));
                
                //3. add page to relevant pdf                                
                if(i > 1)               
                {                                                                                              
                    //if any eurofins sample barcodes are detected then we can start new document   
                    if (barcodeDocs.Count > 0 && barcodeDocs.Any(x => x.IsSampleBarcode))
                    {                       
                        newPdf?.Close();
                        fs = new FileStream(processedFileName, FileMode.Create);
                        newPdf = new Document(PageSize.A4);
                        copy = new PdfCopy(newPdf, fs);
                        newPdf.Open();
                        copy.AddPage(copy.GetImportedPage(reader, i));                        
                        logger.Trace($"Create New Pdf file: {processedFileName}");
                        logger.Debug($"Barcodes on page {i}: {JsonConvert.SerializeObject(detectedBarcodes)}");
                    }
                    else
                    {
                        copy?.AddPage(copy.GetImportedPage(reader, i));
                        logger.Debug($"No Eurofins Barcodes detected on page {i}");
                    }
                }
                else
                {
                    copy?.AddPage(copy.GetImportedPage(reader, 1));
                    logger.Debug($"Barcodes on page {i}: {JsonConvert.SerializeObject(detectedBarcodes)}");
                }
                                
                Console.WriteLine($"Page {i}: {JsonConvert.SerializeObject(detectedBarcodes)}");
                //4. update database with sample document details                
                DocumentProcessing.UpdateDatabaseManager(barcodeDocs);                
            }
            newPdf?.Close(); //close the final new pdf
            copy?.Close();    //this also closes the associated filestream   
            reader.Close(); //close the original file
            
            //move file to archive folder            
            var archiveFileLocation = $@"{archiveLocation}\{fileNameWithExtension}";
            DirectoryHelper.MoveFile(file, archiveFileLocation);
            logger.Trace($"File {fileNameWithExtension} moved to archive folder");
        }
               
        private static IEnumerable<byte[]> GetImagesFromPdfPage(PdfReader pdf, int pageNumber)
        {
            var imagesBytes = new List<byte[]>();           
            var page = pdf.GetPageN(pageNumber);
            var res = (PdfDictionary)PdfReader.GetPdfObject(page.Get(PdfName.RESOURCES));
            var xobj = (PdfDictionary)PdfReader.GetPdfObject(res.Get(PdfName.XOBJECT));
            if (xobj == null) { return null; }
                foreach (var name in xobj.Keys)
                {
                    var obj = xobj.Get(name);
                    if (!obj.IsIndirect()) { continue; }
                    var tg = (PdfDictionary)PdfReader.GetPdfObject(obj);
                    var type = (PdfName)PdfReader.GetPdfObject(tg.Get(PdfName.SUBTYPE));
                    if (!type.Equals(PdfName.IMAGE)) { continue; }

                    var xrefIndex = ((PRIndirectReference)obj).Number;

                    var pdfImage = new iTextSharp.text.pdf.parser.PdfImageObject((PRStream)pdf.GetPdfObject(xrefIndex));
                    var img = pdfImage.GetDrawingImage();

                    var ms = new MemoryStream();

                    var myImageCodecInfo = GetEncoderInfo("image/jpeg");
                    var myEncoder = Encoder.Quality;
                    var myEncoderParameters = new EncoderParameters(1);
                    var myEncoderParameter = new EncoderParameter(myEncoder, 75L);
                    myEncoderParameters.Param[0] = myEncoderParameter;

                    img.Save(ms, myImageCodecInfo, myEncoderParameters);
                    // img.Save(ms, ImageFormat.Jpeg);
                    ms.Position = 0;
                    imagesBytes.Add(ms.ToArray());
                }
            return imagesBytes;
        }
        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            int j;
            var encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        private static IEnumerable<string> ScanImageWithSpire(IEnumerable<byte[]> imageBytes)
        {                      
            foreach (var im in imageBytes)
            {
                using (var image = new MemoryStream(im))
                {
                    var scanningResult = Spire.Barcode.BarcodeScanner.Scan(image);                                        
                    return scanningResult.Distinct();
                }
            }
            return null;
        }
    }
}
