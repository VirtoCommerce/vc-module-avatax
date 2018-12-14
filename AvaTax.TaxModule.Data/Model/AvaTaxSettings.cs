using System.Collections.Generic;
using AvaTax.TaxModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Web.Services
{
    public class AvaTaxSettings : IAvaTaxSettings
    {
        public static AvaTaxSettings FromSettings(IEnumerable<SettingEntry> settings)
        {
            return new AvaTaxSettings
            {
                AccountNumber = settings.GetSettingValue("Avalara.Tax.Credentials.AccountNumber", string.Empty),
                LicenseKey = settings.GetSettingValue("Avalara.Tax.Credentials.LicenseKey", string.Empty),
                CompanyCode = settings.GetSettingValue("Avalara.Tax.Credentials.CompanyCode", string.Empty),
                ServiceUrl = settings.GetSettingValue("Avalara.Tax.Credentials.ServiceUrl", string.Empty),
                IsEnabled = settings.GetSettingValue("Avalara.Tax.IsEnabled", false)
            };
        }

        public virtual bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(AccountNumber)
                  && !string.IsNullOrEmpty(LicenseKey)
                  && !string.IsNullOrEmpty(ServiceUrl)
                  && !string.IsNullOrEmpty(CompanyCode);
            }
        }

        public string AccountNumber { get; set; }
        public string LicenseKey { get; set; }
        public string CompanyCode { get; set; }
        public string ServiceUrl { get; set; }
        public bool IsEnabled { get; set; } = false;
    }
}