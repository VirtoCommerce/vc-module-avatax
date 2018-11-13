namespace AvaTax.TaxModule.Web.Services
{
    public interface ITaxSettings
    {
        int AccountNumber { get; }
        string LicenseKey { get; }
        bool IsEnabled { get; }
        string ServiceUrl { get; }
        string CompanyCode { get; }
    }
}
