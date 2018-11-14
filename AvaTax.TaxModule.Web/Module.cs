using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Web.Services;
using Common.Logging;
using Microsoft.Practices.Unity;
using System;
using VirtoCommerce.Domain.Tax.Services;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Web
{
    public class Module : ModuleBase
    {
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
            AvaTaxClient ClientFactory(ITaxSettings taxSettings)
            {
                var machineName = Environment.MachineName;
                var avaTaxUri = new Uri(taxSettings.ServiceUrl);
                var result = new AvaTaxClient(ApplicationName, ApplicationVersion, machineName, avaTaxUri)
                    .WithSecurity(taxSettings.AccountNumber, taxSettings.LicenseKey);

                return result;
            }

            _container.RegisterInstance<Func<ITaxSettings, AvaTaxClient>>(ClientFactory);
        }

        public override void PostInitialize()
        {
            var settingManager = _container.Resolve<ISettingsManager>();
            var taxService = _container.Resolve<ITaxService>();
            var moduleSettings = settingManager.GetModuleSettings("Avalara.Tax");

            taxService.RegisterTaxProvider(() => new AvaTaxRateProvider(_container.Resolve<ILog>(),
                _container.Resolve<Func<ITaxSettings, AvaTaxClient>>(), moduleSettings)
            {
                Name = "Avalara taxes",
                Description = "Avalara service integration",
                LogoUrl = "Modules/$(Avalara.Tax)/Content/400.png"
            });
        }
        #endregion
    }
}