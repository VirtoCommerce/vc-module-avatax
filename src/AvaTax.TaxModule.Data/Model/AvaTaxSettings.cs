using System.Collections.Generic;
using AvaTax.TaxModule.Core;
using AvaTax.TaxModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Web.Services
{
    public class AvaTaxSettings : IAvaTaxSettings
    {
        public static AvaTaxSettings FromSettings(IEnumerable<ObjectSettingEntry> settings, AvaTaxSecureOptions options)
        {
            return new AvaTaxSettings
            {
                AccountNumber = options.AccountNumber,
                LicenseKey = options.LicenseKey,
                CompanyCode = settings.GetValue<string>(ModuleConstants.Settings.Credentials.CompanyCode),
                ServiceUrl = settings.GetValue<string>(ModuleConstants.Settings.Credentials.ServiceUrl),
                AdminAreaUrl = settings.GetValue<string>(ModuleConstants.Settings.Credentials.AdminAreaUrl),
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
        public string AdminAreaUrl { get; set; }
        public Address SourceAddress { get; set; }
    }
}
