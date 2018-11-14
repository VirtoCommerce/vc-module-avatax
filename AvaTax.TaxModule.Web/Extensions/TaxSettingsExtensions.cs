using AvaTax.TaxModule.Web.Services;

namespace AvaTax.TaxModule.Web.Extensions
{
    public static class TaxSettingsExtensions
    {
        public static bool CredentialsAreFilled(this ITaxSettings settings)
        {
            return settings.AccountNumber != 0
                   && !string.IsNullOrEmpty(settings.LicenseKey)
                   && !string.IsNullOrEmpty(settings.ServiceUrl)
                   && !string.IsNullOrEmpty(settings.CompanyCode);
        }
    }
}