using System;
using System.Threading.Tasks;
using AvaTax.TaxModule.Core.Models;
using VirtoCommerce.Platform.Core.Common;

namespace AvaTax.TaxModule.Core.Services
{
    public interface IOrdersSynchronizationService
    {
        Task<AvaTaxOrderSynchronizationStatus> GetOrderSynchronizationStatusAsync(string orderId);
        Task SynchronizeOrdersAsync(string[] orderIds, Action<AvaTaxOrdersSynchronizationProgress> progressCallback, ICancellationToken cancellationToken);
    }
}