using System;
using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Web.Services;
using Common.Logging;
using Microsoft.Practices.Unity;
using VirtoCommerce.Domain.Customer.Services;
using VirtoCommerce.Domain.Tax.Services;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Web
{
    public class Module : ModuleBase
    {
        private const string _usernamePropertyName = "Avalara.Tax.Credentials.AccountNumber";
        private const string _passwordPropertyName = "Avalara.Tax.Credentials.LicenseKey";
        private const string _serviceUrlPropertyName = "Avalara.Tax.Credentials.ServiceUrl";
        private const string _companyCodePropertyName = "Avalara.Tax.Credentials.CompanyCode";
        private const string _isEnabledPropertyName = "Avalara.Tax.IsEnabled";
        private const string _isValidateAddressPropertyName = "Avalara.Tax.IsValidateAddress";

        private const string ApplicationName = "AvaTax.TaxModule for VirtoCommerce";
        private const string ApplicationVersion = "2.x";

        private readonly IUnityContainer _container;

        public Module(IUnityContainer container)
        {
            _container = container;
        }

        #region IModule Members

        public override void Initialize()
        {
            var settingsManager = _container.Resolve<ISettingsManager>();

            var avalaraTax = new AvaTaxSettings(_usernamePropertyName, _passwordPropertyName, _serviceUrlPropertyName, _companyCodePropertyName, 
                _isEnabledPropertyName, _isValidateAddressPropertyName, settingsManager);

            _container.RegisterInstance<ITaxSettings>(avalaraTax);

            object ClientFactory(IUnityContainer container)
            {
                var machineName = Environment.MachineName;
                var avaTaxUri = new Uri(avalaraTax.ServiceUrl);
                var result = new AvaTaxClient(ApplicationName, ApplicationVersion, machineName, avaTaxUri)
                    .WithSecurity(avalaraTax.Username, avalaraTax.Password);

                return result;
            }

            _container.RegisterType<AvaTaxClient>(new InjectionFactory(ClientFactory));
        }

        public override void PostInitialize()
        {
            var settingManager = _container.Resolve<ISettingsManager>();
            var taxService = _container.Resolve<ITaxService>();
            var moduleSettings = settingManager.GetModuleSettings("Avalara.Tax");
            taxService.RegisterTaxProvider(() => new AvaTaxRateProvider(_container.Resolve<IMemberService>(), _container.Resolve<ILog>(), 
                _container.Resolve<Func<AvaTaxClient>>(), moduleSettings)
            {
                Name = "Avalara taxes",
                Description = "Avalara service integration",
                LogoUrl = "Modules/$(Avalara.Tax)/Content/400.png"
            });
        }
        #endregion
    }
}