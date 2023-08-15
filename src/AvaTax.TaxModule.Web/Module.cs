using System;
using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Core;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data.Providers;
using AvaTax.TaxModule.Data.Services;
using AvaTax.TaxModule.Web.BackgroundJobs;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.TaxModule.Core.Model;
using ModuleConstants = AvaTax.TaxModule.Core.ModuleConstants;

namespace AvaTax.TaxModule.Web
{
    public class Module : IModule, IHasConfiguration
    {
        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }

        private const string _applicationName = "AvaTax.TaxModule for VirtoCommerce";
        private const string _applicationVersion = "3.x";

        public void Initialize(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<Func<IAvaTaxSettings, AvaTaxClient>>(provider => settings =>
            {
                var machineName = Environment.MachineName;
                var avaTaxUri = new Uri(settings.ServiceUrl);
                var result = new AvaTaxClient(_applicationName, _applicationVersion, machineName, avaTaxUri)
                    .WithSecurity(settings.AccountNumber, settings.LicenseKey);

                return result;
            });

            serviceCollection.AddTransient<IAddressValidationService, AddressValidationService>();
            serviceCollection.AddTransient<IOrdersSynchronizationService, OrdersSynchronizationService>();
            serviceCollection.AddTransient<IOrderTaxTypeResolver, OrderTaxTypeResolver>();

            serviceCollection.AddOptions<AvaTaxSecureOptions>().Bind(Configuration.GetSection("Tax:Avalara")).ValidateDataAnnotations();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);

            var taxProviderRegistrar = appBuilder.ApplicationServices.GetRequiredService<ITaxProviderRegistrar>();
            taxProviderRegistrar.RegisterTaxProvider(() =>
            {
                var avalaraOptions = appBuilder.ApplicationServices.GetRequiredService<IOptions<AvaTaxSecureOptions>>();
                var logger = appBuilder.ApplicationServices.GetRequiredService<ILogger<AvaTaxRateProvider>>();
                var avaTaxClientFactory = appBuilder.ApplicationServices.GetRequiredService<Func<IAvaTaxSettings, AvaTaxClient>>();
                return new AvaTaxRateProvider(logger, avaTaxClientFactory, avalaraOptions);

            });
            settingsRegistrar.RegisterSettingsForType(ModuleConstants.Settings.AllSettings, nameof(AvaTaxRateProvider));

            var permissionsRegistrar = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
            permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "Avalara Tax", ModuleConstants.Security.Permissions.AllPermissions);

            var settingsManager = appBuilder.ApplicationServices.GetRequiredService<ISettingsManager>();

            var processJobEnabled = settingsManager.GetValue<bool>(ModuleConstants.Settings.ScheduledOrdersSynchronization.SynchronizationIsEnabled);
            if (processJobEnabled)
            {
                var cronExpression = settingsManager.GetValue<string>(ModuleConstants.Settings.ScheduledOrdersSynchronization.SynchronizationCronExpression);
                RecurringJob.AddOrUpdate<OrdersSynchronizationJob>("SendOrdersToAvaTaxJob", x => x.RunScheduled(JobCancellationToken.Null, null), cronExpression);
            }
            else
            {
                RecurringJob.RemoveIfExists("SendOrdersToAvaTaxJob");
            }
        }
        public void Uninstall()
        {
            // Nothing to do here
        }
    }
}
