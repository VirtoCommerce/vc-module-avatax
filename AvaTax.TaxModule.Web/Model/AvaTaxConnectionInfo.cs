using AvaTax.TaxModule.Web.Services;

namespace AvaTax.TaxModule.Web.Model
{
    public class AvaTaxConnectionInfo : ITaxSettings
    {
        public int AccountNumber { get; set; }
        public string LicenseKey { get; set; }
        public bool IsEnabled { get; set; }
        public string ServiceUrl { get; set; }
        public string CompanyCode { get; set; }
    }
}