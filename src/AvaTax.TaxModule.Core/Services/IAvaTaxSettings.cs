namespace AvaTax.TaxModule.Core.Services
{
    /// <summary>
    /// Represents the settings for Avalara API connection endpoint
    /// </summary>
    public interface IAvaTaxSettings
    {
        bool IsValid { get; }
        string AccountNumber { get; }
        string LicenseKey { get; }
        string ServiceUrl { get; }
        string AdminAreaUrl { get; }
        string CompanyCode { get; }
    }
}
