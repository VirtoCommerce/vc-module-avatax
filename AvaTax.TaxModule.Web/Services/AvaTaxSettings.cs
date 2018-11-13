using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Web.Services
{
    public class AvaTaxSettings : ITaxSettings
    {
        private const string AccountNumberPropertyName = "Avalara.Tax.Credentials.AccountNumber";
        private const string LicenseKeyPropertyName = "Avalara.Tax.Credentials.LicenseKey";
        private const string ServiceUrlPropertyName = "Avalara.Tax.Credentials.ServiceUrl";
        private const string CompanyCodePropertyName = "Avalara.Tax.Credentials.CompanyCode";
        private const string IsEnabledPropertyName = "Avalara.Tax.IsEnabled";

        private readonly ISettingsManager _settingsManager;

        public AvaTaxSettings(ISettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        public int AccountNumber => _settingsManager.GetValue(AccountNumberPropertyName, 0);
        public string LicenseKey => _settingsManager.GetValue(LicenseKeyPropertyName, string.Empty);
        public string ServiceUrl => _settingsManager.GetValue(ServiceUrlPropertyName, string.Empty);
        public string CompanyCode => _settingsManager.GetValue(CompanyCodePropertyName, string.Empty);
        public bool IsEnabled => _settingsManager.GetValue(IsEnabledPropertyName, true);
    }
}