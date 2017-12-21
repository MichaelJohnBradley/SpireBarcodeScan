namespace SpireBarcodeScan
{
    public class BarcodeDocument
    {
        public string Barcode { get; set; }         
        public string ProcessedFilePath { get; set; }
        public string FileName { get; set; }
        public bool IsSampleBarcode { get; set; }
    }
}