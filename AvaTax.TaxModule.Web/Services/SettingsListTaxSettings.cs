using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Web.Services
{
    public class SettingsListTaxSettings : ITaxSettings
    {
        private const string AccountNumberPropertyName = "Avalara.Tax.Credentials.AccountNumber";
        private const string LicenseKeyPropertyName = "Avalara.Tax.Credentials.LicenseKey";
        private const string ServiceUrlPropertyName = "Avalara.Tax.Credentials.ServiceUrl";
        private const string CompanyCodePropertyName = "Avalara.Tax.Credentials.CompanyCode";
        private const string IsEnabledPropertyName = "Avalara.Tax.IsEnabled";

        private readonly IEnumerable<SettingEntry> _settings;

        public SettingsListTaxSettings(IEnumerable<SettingEntry> settings)
        {
            _settings = settings;
        }

        private string GetSettingValue(string settingName, string defaultValue)
        {
            var result = defaultValue;

            var foundSetting = _settings.FirstOrDefault(setting => setting.Name == settingName);
            if (foundSetting != null)
            {
                result = foundSetting.Value;
            }

            return result;
        }

        public int AccountNumber => int.Parse(GetSettingValue(AccountNumberPropertyName, "0"));
        public string LicenseKey => GetSettingValue(LicenseKeyPropertyName, string.Empty);
        public string CompanyCode => GetSettingValue(CompanyCodePropertyName, string.Empty);
        public string ServiceUrl => GetSettingValue(ServiceUrlPropertyName, string.Empty);
        public bool IsEnabled => bool.Parse(GetSettingValue(IsEnabledPropertyName, "false"));
    }
}