using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Core;
using AvaTax.TaxModule.Core.Models;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data.Model;
using AvaTax.TaxModule.Data.Providers;
using AvaTax.TaxModule.Web.Services;
using Microsoft.Extensions.Options;
using VirtoCommerce.AvalaraTaxModule.Data.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.TaxModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Model.Search;
using VirtoCommerce.TaxModule.Core.Services;

namespace AvaTax.TaxModule.Data.Services
{
    public class AddressValidationService : IAddressValidationService
    {
        private readonly IStoreService _storeService;
        private readonly Func<IAvaTaxSettings, AvaTaxClient> _avaTaxClientFactory;
        private readonly ITaxProviderSearchService _taxProviderSearchService;
        private readonly AvaTaxSecureOptions _options;

        public AddressValidationService(
            IStoreService storeService,
            Func<IAvaTaxSettings, AvaTaxClient> avaTaxClientFactory,
            ITaxProviderSearchService taxProviderSearchService,
            IOptions<AvaTaxSecureOptions> options)
        {
            _storeService = storeService;
            _avaTaxClientFactory = avaTaxClientFactory;
            _taxProviderSearchService = taxProviderSearchService;
            _options = options.Value;
        }

        public async Task<AddressValidationResult> ValidateAddressAsync(Address address, string storeId)
        {
            var store = await _storeService.GetByIdAsync(storeId);
            if (store == null)
            {
                throw new ArgumentException("Store with specified storeId does not exist.", nameof(storeId));
            }

            var taxProviderSearchCriteria = new TaxProviderSearchCriteria
            {
                StoreIds = new[] { store.Id },
                Keyword = nameof(AvaTaxRateProvider)
            };
            var avaTaxProviders = await _taxProviderSearchService.SearchAsync(taxProviderSearchCriteria);
            var avaTaxProvider = avaTaxProviders.Results.FirstOrDefault(x => x.IsActive);

            if (avaTaxProvider == null)
            {
                throw new ArgumentException($"Store '{storeId}' does not use AvaTaxRateProvider, so it can't be used for address validation.");
            }

            var avaTaxSettings = AvaTaxSettings.FromSettings(avaTaxProvider.Settings, _options);

            var addressValidationInfo = AbstractTypeFactory<AvaAddressValidationInfo>.TryCreateInstance();
            addressValidationInfo.FromAddress(address);

            var avaTaxClient = _avaTaxClientFactory(avaTaxSettings);
            bool addressIsValid;
            var messages = new List<string>();
            var validatedAddresess = new List<Address>();

            try
            {
                var addressResolutionModel = await avaTaxClient.ResolveAddressPostAsync(addressValidationInfo);

                // If the address cannot be resolved, it's coordinates will be null.
                // This might mean that the address is invalid.
                addressIsValid = addressResolutionModel.coordinates != null;

                if (!addressResolutionModel.messages.IsNullOrEmpty())
                {
                    messages.AddRange(addressResolutionModel.messages.Select(x => $"{x.summary} {x.details}"));
                }

                if (!addressResolutionModel.validatedAddresses.IsNullOrEmpty())
                {
                    validatedAddresess.AddRange(addressResolutionModel.validatedAddresses.Select(x => x.ToAddress()));
                }
            }
            catch (AvaTaxError e)
            {
                addressIsValid = false;

                var errorResult = e.error.error;
                if (!errorResult.details.IsNullOrEmpty())
                {
                    messages.AddRange(errorResult.details.Select(x => x.description));
                }
            }

            return new AddressValidationResult
            {
                Address = address,
                ValidatedAddresses = validatedAddresess,
                AddressIsValid = addressIsValid,
                Messages = messages.ToArray()
            };
        }
    }
}
