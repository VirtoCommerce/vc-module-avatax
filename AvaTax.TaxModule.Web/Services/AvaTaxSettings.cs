using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Web.Services
{
    public class AvaTaxSettings : ITaxSettings
    {
        private const string UsernamePropertyName = "Avalara.Tax.Credentials.AccountNumber";
        private const string PasswordPropertyName = "Avalara.Tax.Credentials.LicenseKey";
        private const string ServiceUrlPropertyName = "Avalara.Tax.Credentials.ServiceUrl";
        private const string CompanyCodePropertyName = "Avalara.Tax.Credentials.CompanyCode";
        private const string IsEnabledPropertyName = "Avalara.Tax.IsEnabled";

        private readonly ISettingsManager _settingsManager;

        public AvaTaxSettings(ISettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        public string Username
        {

            get
            {
                var retVal = _settingsManager.GetValue(UsernamePropertyName, string.Empty);
                return retVal;
            }
        }

        public string Password
        {
            get
            {
                var retVal = _settingsManager.GetValue(PasswordPropertyName, string.Empty);
                return retVal;
            }
        }
            
        public string ServiceUrl
        {
            get
            {
                var retVal = _settingsManager.GetValue(ServiceUrlPropertyName, string.Empty);
                return retVal;
            }
        }

        public string CompanyCode
        {
            get 
            {
                var retVal = _settingsManager.GetValue(CompanyCodePropertyName, string.Empty);
                return retVal;
            }
        }

        public bool IsEnabled
        {
            get
            {
                var retVal = _settingsManager.GetValue(IsEnabledPropertyName, true);
                return retVal;
            }
        }
    }
}