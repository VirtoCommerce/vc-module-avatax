using AvaTax.TaxModule.Core;
using AvaTax.TaxModule.Core.Services;
using System.Collections.Generic;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Web.Services
{
    public class AvaTaxSettings : IAvaTaxSettings
    {
        public static AvaTaxSettings FromSettings(IEnumerable<ObjectSettingEntry> settings)
        {
            return new AvaTaxSettings
            {
                AccountNumber = settings.GetSettingValue(ModuleConstants.Settings.Credentials.AccountNumber.Name, string.Empty),
                LicenseKey = settings.GetSettingValue(ModuleConstants.Settings.Credentials.LicenseKey.Name, string.Empty),
                CompanyCode = settings.GetSettingValue(ModuleConstants.Settings.Credentials.CompanyCode.Name, string.Empty),
                ServiceUrl = settings.GetSettingValue(ModuleConstants.Settings.Credentials.ServiceUrl.Name, string.Empty),
                AdminAreaUrl = settings.GetSettingValue(ModuleConstants.Settings.Credentials.AdminAreaUrl.Name, string.Empty),
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
        public bool IsEnabled { get; set; } = false;
    }
}
