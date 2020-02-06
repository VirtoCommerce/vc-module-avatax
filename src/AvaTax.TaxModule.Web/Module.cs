using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Core;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data;
using AvaTax.TaxModule.Data.Services;
using AvaTax.TaxModule.Web.BackgroundJobs;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvaTax.TaxModule.Data.Providers;
using Microsoft.Extensions.Logging;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.TaxModule.Core.Model;
using ModuleConstants = AvaTax.TaxModule.Core.ModuleConstants;

namespace AvaTax.TaxModule.Web
{
    public class Module : IModule, IExportSupport, IImportSupport
    {
        public void Uninstall()
        {
            throw new NotImplementedException();
        }

        public ManifestModuleInfo ModuleInfo { get; set; }
        private IApplicationBuilder _appBuilder;

        private const string ApplicationName = "AvaTax.TaxModule for VirtoCommerce";
        private const string ApplicationVersion = "3.x";

        public void Initialize(IServiceCollection serviceCollection)
        {
            var snapshot = serviceCollection.BuildServiceProvider();

            
            serviceCollection.AddTransient<Func<IAvaTaxSettings, AvaTaxClient>>(provider => settings =>
            {
                var machineName = Environment.MachineName;
                var avaTaxUri = new Uri(settings.ServiceUrl);
                var result = new AvaTaxClient(ApplicationName, ApplicationVersion, machineName, avaTaxUri)
                    .WithSecurity(settings.AccountNumber, settings.LicenseKey);

                return result;
            });

            serviceCollection.AddTransient<IAddressValidationService, AddressValidationService>();
            serviceCollection.AddTransient<IOrdersSynchronizationService, OrdersSynchronizationService>();
            serviceCollection.AddTransient<IOrderTaxTypeResolver, OrderTaxTypeResolver>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            _appBuilder = appBuilder;

            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);

            var taxProviderRegistrar = appBuilder.ApplicationServices.GetRequiredService<ITaxProviderRegistrar>();
            taxProviderRegistrar.RegisterTaxProvider(() =>
            {
                var logger = appBuilder.ApplicationServices.GetRequiredService<ILogger<AvaTaxRateProvider>>();
                var avaTaxClientFactory = appBuilder.ApplicationServices.GetRequiredService<Func<IAvaTaxSettings, AvaTaxClient>>();
                return new AvaTaxRateProvider(logger,avaTaxClientFactory);

            });
            settingsRegistrar.RegisterSettingsForType(ModuleConstants.Settings.AllSettings, nameof(AvaTaxRateProvider));

            var permissionsProvider = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
            permissionsProvider.RegisterPermissions(ModuleConstants.Security.Permissions.AllPermissions.Select(x => new Permission() { GroupName = "Avalara Tax", Name = x }).ToArray());

            var settingManager = appBuilder.ApplicationServices.GetRequiredService<ISettingsManager>();

            var processJobEnabled = settingManager.GetValue(ModuleConstants.Settings.ScheduledOrderSynchronization.IsEnabled, false);
            if (processJobEnabled)
            {
                var cronExpression = settingManager.GetValue(ModuleConstants.Settings.ScheduledOrderSynchronization.CronExpression, "0 0 * * *");
                RecurringJob.AddOrUpdate<OrdersSynchronizationJob>("SendOrdersToAvaTaxJob", x => x.RunScheduled(JobCancellationToken.Null, null), cronExpression);
            }
            else
            {
                RecurringJob.RemoveIfExists("SendOrdersToAvaTaxJob");
            }
        }

        public Task ExportAsync(Stream outStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback,
            ICancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task ImportAsync(Stream inputStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback,
            ICancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
