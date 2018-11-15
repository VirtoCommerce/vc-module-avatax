namespace AvaTax.TaxModule.Web.Services
{
    /// <summary>
    /// Represents the settings for Avalara API connection endpoint
    /// </summary>
    public interface IAvaTaxSettings
    {
        bool IsValid { get; }
        string AccountNumber { get; }
        string LicenseKey { get; }
        bool IsEnabled { get; }
        string ServiceUrl { get; }
        string CompanyCode { get; }
    }
}
