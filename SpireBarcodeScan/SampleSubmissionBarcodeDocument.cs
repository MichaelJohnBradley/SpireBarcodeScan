namespace SpireBarcodeScan
{
    public partial class SampleSubmissionBarcodeDocument
    {
        public int Id { get; set; }
        public string Barcode { get; set; }
        public string StoredLocation { get; set; }
        public System.DateTime ProcessedOnDate { get; set; }
    }
}
