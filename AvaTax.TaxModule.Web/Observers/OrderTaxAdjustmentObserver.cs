using System;
using System.Linq;
using Common.Logging;
using VirtoCommerce.Domain.Order.Events;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Domain.Store.Services;

namespace AvaTax.TaxModule.Web.Observers
{

    public class OrderTaxAdjustmentObserver : IObserver<OrderChangeEvent>
    {
        private readonly IStoreService _storeService;


        public OrderTaxAdjustmentObserver(IStoreService storeService)
        {
            _storeService = storeService;
        }

        #region IObserver<CustomerOrder> Members

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(OrderChangeEvent value)
        {
            foreach (var entry in value.ChangedEntries)
            {
                if (entry.EntryState == EntryState.Added || entry.EntryState == EntryState.Modified)
                {
                    var order = entry.NewEntry;
                    var store = _storeService.GetById(order.StoreId);
                    var taxProvider = store.TaxProviders.FirstOrDefault(x => x.Code == typeof(AvaTaxRateProvider).Name);
                    if (taxProvider != null && taxProvider.IsActive)
                    {
                        (taxProvider as AvaTaxRateProvider).CalculateOrderTax(order);
                    }
                }
            }
        }

        #endregion
    }
}