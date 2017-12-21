using System.Data.Entity;

namespace SpireBarcodeScan
{
    public partial class DatabaseEntities : DbContext
    {
        public DatabaseEntities(string sConnectionString)
            : base(sConnectionString)
        {
        }
    }
}