using System;
using System.Linq;
using VirtoCommerce.Domain.Cart.Events;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Common.Events;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Domain.Store.Services;

namespace AvaTax.TaxModule.Web.Observers
{

    public class CartTaxAdjustmentObserver : IObserver<CartChangeEvent>
    {
        private readonly IStoreService _storeService;

        public CartTaxAdjustmentObserver(IStoreService storeService)
        {
            _storeService = storeService;
        }

        #region IObserver<ShoppingCart> Members

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(CartChangeEvent value)
        {
            foreach (var entry in value.ChangedEntries)
            {
                if (entry.EntryState == EntryState.Modified || entry.EntryState == EntryState.Added)
                {
                    CalculateCustomerOrderTaxes(entry.NewEntry);
                }
            }
        }

        #endregion

        private void CalculateCustomerOrderTaxes(ShoppingCart cart)
        {
            var store = _storeService.GetById(cart.StoreId);
            var taxProvider = store.TaxProviders.FirstOrDefault(x => x.Code == typeof(AvaTaxRateProvider).Name);
            if (taxProvider != null && taxProvider is AvaTaxRateProvider && taxProvider.IsActive)
            {
                (taxProvider as AvaTaxRateProvider).CalculateCartTax(cart);
            }
        }
    }
}