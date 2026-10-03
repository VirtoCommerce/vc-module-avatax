using System;
using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Core;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data.Providers;
using AvaTax.TaxModule.Data.Services;
using AvaTax.TaxModule.Web.BackgroundJobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Jobs;
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

            // Scheduled order synchronization. It used to be added or removed once in PostInitialize from the settings read at
            // startup; now the background-job engine re-evaluates it whenever the enabler or cron setting changes. The id is the
            // one the Hangfire recurring job used, so on the Hangfire engine this replaces the old entry. The same handler also
            // serves manual runs from the admin UI.
            serviceCollection.AddRecurringJob<OrdersSynchronizationJob, OrdersSynchronizationJobPayload>(schedule => schedule
                .WithId("SendOrdersToAvaTaxJob")
                .FromSettings(
                    ModuleConstants.Settings.ScheduledOrdersSynchronization.SynchronizationIsEnabled,
                    ModuleConstants.Settings.ScheduledOrdersSynchronization.SynchronizationCronExpression));

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
        }
        public void Uninstall()
        {
            // Nothing to do here
        }
    }
}
