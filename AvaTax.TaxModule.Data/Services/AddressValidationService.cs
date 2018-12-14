using System;
using System.Collections.Generic;
using System.Linq;
using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Core.Models;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data.Model;
using AvaTax.TaxModule.Web.Services;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Common;

namespace AvaTax.TaxModule.Data.Services
{
    public class AddressValidationService : IAddressValidationService
    {
        private readonly IStoreService _storeService;
        private readonly Func<IAvaTaxSettings, AvaTaxClient> _avaTaxClientFactory;

        public AddressValidationService(IStoreService storeService, Func<IAvaTaxSettings, AvaTaxClient> avaTaxClientFactory)
        {
            _storeService = storeService;
            _avaTaxClientFactory = avaTaxClientFactory;
        }

        public AddressValidationResult ValidateAddress(Address address, string storeId)
        {
            var store = _storeService.GetById(storeId);
            if (store == null)
            {
                throw new ArgumentException("Store with specified storeId does not exist.", nameof(storeId));
            }

            var avaTaxProvider = store.TaxProviders?.FirstOrDefault(x => x.Code == "AvaTaxRateProvider" && x.IsActive);
            if (avaTaxProvider == null)
            {
                throw new ArgumentException($"Store '{storeId}' does not use AvaTaxRateProvider, so it can't be used for address validation.");
            }

            var avaTaxSettings = AvaTaxSettings.FromSettings(avaTaxProvider.Settings);

            var addressValidationInfo = AbstractTypeFactory<AvaAddressValidationInfo>.TryCreateInstance();
            addressValidationInfo.FromAddress(address);

            var avaTaxClient = _avaTaxClientFactory(avaTaxSettings);
            bool addressIsValid;
            var messages = new List<string>();

            try
            {
                var addressResolutionModel = avaTaxClient.ResolveAddressPost(addressValidationInfo);

                // If the address cannot be resolved, it's coordinates will be null.
                // This might mean that the address is invalid.
                addressIsValid = addressResolutionModel.coordinates != null;

                if (!addressResolutionModel.messages.IsNullOrEmpty())
                {
                    messages.AddRange(addressResolutionModel.messages.Select(x => $"{x.summary} {x.details}"));
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
                AddressIsValid = addressIsValid,
                Messages = messages.ToArray()
            };
        }
    }
}