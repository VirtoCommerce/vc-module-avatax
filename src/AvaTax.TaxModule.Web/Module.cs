using System;
using System.Linq;
using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Core;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data.Providers;
using AvaTax.TaxModule.Data.Repositories;
using AvaTax.TaxModule.Data.Services;
using AvaTax.TaxModule.Web.BackgroundJobs;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.Extensions;
using VirtoCommerce.TaxModule.Core.Model;
using ModuleConstants = AvaTax.TaxModule.Core.ModuleConstants;

namespace AvaTax.TaxModule.Web
{
    public class Module : IModule, IHasConfiguration
    {
        public void Uninstall()
        {
            throw new NotImplementedException();
        }

        public ManifestModuleInfo ModuleInfo { get; set; }

        public IConfiguration Configuration { get; set; }

        private const string ApplicationName = "AvaTax.TaxModule for VirtoCommerce";
        private const string ApplicationVersion = "3.x";

        public void Initialize(IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<AvaTaxDbContext>((provider, options) =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                options.UseSqlServer(configuration.GetConnectionString(ModuleInfo.Id) ?? configuration.GetConnectionString("VirtoCommerce"));
            });

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

            var permissionsProvider = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
            permissionsProvider.RegisterPermissions(ModuleConstants.Security.Permissions.AllPermissions.Select(x => new Permission() { GroupName = "Avalara Tax", Name = x }).ToArray());

            var settingManager = appBuilder.ApplicationServices.GetRequiredService<ISettingsManager>();

            var processJobEnabled = settingManager.GetValue(ModuleConstants.Settings.ScheduledOrdersSynchronization.SynchronizationIsEnabled.Name, false);
            if (processJobEnabled)
            {
                var cronExpression = settingManager.GetValue(ModuleConstants.Settings.ScheduledOrdersSynchronization.SynchronizationCronExpression.Name, "0 0 * * *");
                RecurringJob.AddOrUpdate<OrdersSynchronizationJob>("SendOrdersToAvaTaxJob", x => x.RunScheduled(JobCancellationToken.Null, null), cronExpression);
            }
            else
            {
                RecurringJob.RemoveIfExists("SendOrdersToAvaTaxJob");
            }

            using (var serviceScope = appBuilder.ApplicationServices.CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<AvaTaxDbContext>();
                dbContext.Database.MigrateIfNotApplied(MigrationName.GetUpdateV2MigrationName(ModuleInfo.Id));
            }
        }
    }
}
