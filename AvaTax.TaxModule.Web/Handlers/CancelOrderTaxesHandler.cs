using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Order.Events;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;

namespace AvaTax.TaxModule.Web.Handlers
{
    public class CancelOrderTaxesHandler : IEventHandler<OrderChangeEvent>
    {
        private readonly IStoreService _storeService;

        public CancelOrderTaxesHandler(IStoreService storeService)
        {
            _storeService = storeService;
        }

        public Task Handle(OrderChangeEvent message)
        {
            foreach (var entry in message.ChangedEntries)
            {
                if (entry.EntryState == EntryState.Modified)
                {
                    CancelCustomerOrderTaxes(entry.NewEntry);
                }
            }

            return Task.CompletedTask;
        }

        private void CancelCustomerOrderTaxes(CustomerOrder order)
        {
            if (order.IsCancelled)
            {
                var store = _storeService.GetById(order.StoreId);
                var taxProvider = store.TaxProviders.FirstOrDefault(x => x.Code == typeof(AvaTaxRateProvider).Name);
                if (taxProvider != null && taxProvider.IsActive && taxProvider is AvaTaxRateProvider avaTaxRateProvider)
                {
                    avaTaxRateProvider.CancelTaxDocument(order);
                }
            }
        }
    }
}