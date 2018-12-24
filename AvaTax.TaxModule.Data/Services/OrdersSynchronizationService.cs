using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Core;
using AvaTax.TaxModule.Core.Models;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data.Model;
using AvaTax.TaxModule.Web.Services;
using Newtonsoft.Json;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Inventory.Services;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.Domain.Search.ChangeFeed;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Common;
using FulfillmentCenter = VirtoCommerce.Domain.Inventory.Model.FulfillmentCenter;

namespace AvaTax.TaxModule.Data.Services
{
    [CLSCompliant(false)]
    public class OrdersSynchronizationService : IOrdersSynchronizationService
    {
        private const int BatchSize = 50;

        private readonly ICustomerOrderService _orderService;
        private readonly IStoreService _storeService;
        private readonly IFulfillmentCenterService _fulfillmentCenterService;
        private readonly IOrderTaxTypeResolver _orderTaxTypeResolver;
        private readonly Func<IAvaTaxSettings, AvaTaxClient> _avaTaxClientFactory;

        public OrdersSynchronizationService(ICustomerOrderService orderService, IStoreService storeService,
            IFulfillmentCenterService fulfillmentCenterService, IOrderTaxTypeResolver orderTaxTypeResolver,
            Func<IAvaTaxSettings, AvaTaxClient> avaTaxClientFactory)
        {
            _orderService = orderService;
            _storeService = storeService;
            _fulfillmentCenterService = fulfillmentCenterService;
            _orderTaxTypeResolver = orderTaxTypeResolver;
            _avaTaxClientFactory = avaTaxClientFactory;
        }

        public async Task<AvaTaxOrderSynchronizationStatus> GetOrderSynchronizationStatusAsync(string orderId)
        {
            var order = _orderService.GetByIds(new[] { orderId }).FirstOrDefault();
            if (order == null)
            {
                throw new ArgumentException("Order with given ID does not exist.", nameof(orderId));
            }

            var result = AbstractTypeFactory<AvaTaxOrderSynchronizationStatus>.TryCreateInstance();

            var avaTaxSettings = GetAvataxSettingsForOrder(order);
            if (avaTaxSettings != null)
            {
                var avaTaxClient = _avaTaxClientFactory(avaTaxSettings);
                try
                {
                    var companyCode = avaTaxSettings.CompanyCode;
                    var transactionModel = await avaTaxClient.GetTransactionByCodeAsync(companyCode, order.Number, DocumentType.SalesInvoice, string.Empty);
                    result.TransactionId = transactionModel.id;
                    result.LastSynchronizationDate = transactionModel.modifiedDate;
                    result.LinkToAvaTax = BuildLinkToAvaTaxTransaction(transactionModel, avaTaxSettings);
                    result.RawContent = JsonConvert.SerializeObject(transactionModel, Formatting.Indented);
                }
                catch (AvaTaxError e) when (e.statusCode == HttpStatusCode.NotFound)
                {
                    // The transaction does not exist in Avalara - the order had probably never been sent there.
                    // This is normal, and we shouldn't treat it as error.
                    result.LastSynchronizationDate = null;
                }
                catch (AvaTaxError e)
                {
                    var errorDetails = e.error.error;
                    var joinedMessages = string.Join(Environment.NewLine, errorDetails.details.Select(x => $"{x.severity}: {x.message} {x.description}"));

                    var errorMessage = $"{errorDetails.message}{Environment.NewLine}{joinedMessages}";
                    result.Errors = new[] { errorMessage };
                    result.HasErrors = true;
                }
            }
            result.StoreUsesAvaTax = avaTaxSettings != null;

            return result;
        }

        public async Task SynchronizeOrdersAsync(IIndexDocumentChangeFeed ordersFeed, Action<AvaTaxOrdersSynchronizationProgress> progressCallback, ICancellationToken cancellationToken)
        {
            // TODO: how to find order count when ordersFeed.TotalsCount is null?
            var totalCount = (long)ordersFeed.TotalCount;

            var progressInfo = new AvaTaxOrdersSynchronizationProgress
            {
                Message = "Reading orders...",
                TotalCount = totalCount,
                ProcessedCount = 0
            };
            progressCallback(progressInfo);

            cancellationToken?.ThrowIfCancellationRequested();

            for (long i = 0; i < totalCount; i += BatchSize)
            {
                var searchResult = await ordersFeed.GetNextBatch();
                var orderIds = searchResult.Select(x => x.DocumentId).ToArray();
                var orders = _orderService.GetByIds(orderIds);

                foreach (var order in orders)
                {
                    var avaTaxSettings = GetAvataxSettingsForOrder(order);
                    if (avaTaxSettings != null)
                    {
                        _orderTaxTypeResolver.ResolveTaxTypeForOrder(order);

                        var avaTaxClient = _avaTaxClientFactory(avaTaxSettings);
                        try
                        {
                            await SendOrderToAvaTax(order, avaTaxSettings.CompanyCode, avaTaxSettings.SourceAddress, avaTaxClient);
                        }
                        catch (AvaTaxError e)
                        {
                            var errorDetails = e.error.error;
                            var joinedMessages = string.Join(Environment.NewLine,
                                errorDetails.details.Select(x => $"{x.severity}: {x.message} {x.description}"));

                            var errorMessage = $"Order #{order.Number}: {errorDetails.message}{Environment.NewLine}{joinedMessages}";
                            progressInfo.Errors.Add(errorMessage);
                        }
                    }
                    else
                    {
                        var errorMessage = $"Order #{order.Number} was not sent to Avalara, because the order store does not use AvaTax as tax provider.";
                        progressInfo.Errors.Add(errorMessage);
                    }

                    cancellationToken?.ThrowIfCancellationRequested();
                }

                var processedCount = Math.Min(i + orderIds.Length, totalCount);
                progressInfo.ProcessedCount = processedCount;
                progressInfo.Message = $"Processed {processedCount} of {totalCount} orders";
                progressCallback(progressInfo);
            }

            progressInfo.Message = "Orders synchronization completed.";
            progressCallback(progressInfo);
        }

        protected virtual AvaTaxSettings GetAvataxSettingsForOrder(CustomerOrder order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }
            AvaTaxSettings result = null;
            if (!string.IsNullOrEmpty(order.StoreId))
            {
                var store = _storeService.GetById(order.StoreId);
                var avaTaxProvider = store.TaxProviders.FirstOrDefault(x => x.Code == ModuleConstants.AvaTaxRateProviderCode);
                if (avaTaxProvider != null && avaTaxProvider.IsActive)
                {
                    result = AvaTaxSettings.FromSettings(avaTaxProvider.Settings);
                    if (result.SourceAddress == null && store.MainFulfillmentCenterId != null)
                    {
                        result.SourceAddress = _fulfillmentCenterService.GetByIds(new[] { store.MainFulfillmentCenterId }).FirstOrDefault()?.Address;
                    }
                }
            }
            return result;
        }

        protected virtual async Task SendOrderToAvaTax(CustomerOrder order, string companyCode, Address sourceAddress, AvaTaxClient avaTaxClient)
        {
            if (!order.IsCancelled)
            {
                var createOrAdjustTransactionModel = AbstractTypeFactory<AvaCreateOrAdjustTransactionModel>.TryCreateInstance();
                createOrAdjustTransactionModel.FromOrder(order, companyCode, sourceAddress);
                var transactionModel = await avaTaxClient.CreateOrAdjustTransactionAsync(string.Empty, createOrAdjustTransactionModel);
            }
            else
            {
                var voidTransactionModel = new VoidTransactionModel { code = VoidReasonCode.DocVoided };
                var transactionModel = await avaTaxClient.VoidTransactionAsync(companyCode, order.Number, DocumentType.Any, voidTransactionModel);
            }
        }

        protected virtual string BuildLinkToAvaTaxTransaction(TransactionModel transactionModel, IAvaTaxSettings avaTaxSettings)
        {
            string result = null;
            if (!string.IsNullOrEmpty(avaTaxSettings.AdminAreaUrl)
                && transactionModel.id != null
                && transactionModel.companyId != null)
            {
                result = $"{avaTaxSettings.AdminAreaUrl}/cup/a/{avaTaxSettings.AccountNumber}/c/{transactionModel.companyId}/transactions/{transactionModel.id}";
            }

            return result;
        }
    }
}