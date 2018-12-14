using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Data.Logging;
using AvaTax.TaxModule.Web.Services;
using Common.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using AvaTax.TaxModule.Core;
using AvaTax.TaxModule.Core.Models;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data.Services;
using AvaTax.TaxModule.Web.BackgroundJobs;
using AvaTax.TaxModule.Web.Models;
using AvaTax.TaxModule.Web.Models.PushNotifications;
using Hangfire;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Web.Security;

namespace AvaTax.TaxModule.Web.Controller
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [RoutePrefix("api/tax/avatax")]
    public class AvaTaxController : ApiController
    {
        private readonly Func<IAvaTaxSettings, AvaTaxClient> _avaTaxClientFactory;
        private readonly AvalaraLogger _logger;
        private readonly IOrdersSynchronizationService _ordersSynchronizationService;
        private readonly IAddressValidationService _addressValidationService;
        private readonly IPushNotificationManager _pushNotificationManager;
        private readonly IUserNameResolver _userNameResolver;
        private readonly ICustomerOrderService _orderService;
        private readonly IStoreService _storeService;
        
        [CLSCompliant(false)]
        public AvaTaxController(ILog log, Func<IAvaTaxSettings, AvaTaxClient> avaTaxClientFactory, IOrdersSynchronizationService ordersSynchronizationService,
            IAddressValidationService addressValidationService, IPushNotificationManager pushNotificationManager, IUserNameResolver userNameResolver, 
            ICustomerOrderService orderService, IStoreService storeService)
        {
            _logger = new AvalaraLogger(log);
            _avaTaxClientFactory = avaTaxClientFactory;
            _ordersSynchronizationService = ordersSynchronizationService;
            _addressValidationService = addressValidationService;
            _pushNotificationManager = pushNotificationManager;
            _userNameResolver = userNameResolver;
            _orderService = orderService;
            _storeService = storeService;
        }

        [HttpPost]
        [ResponseType(typeof(void))]
        [Route("ping")]
        public IHttpActionResult TestConnection([FromBody]AvaTaxSettings taxSetting)
        {
            IHttpActionResult retVal = BadRequest();
            LogInvoker<AvalaraLogger.TaxRequestContext>.Execute(log =>
            {
                if (taxSetting == null)
                {
                    const string errorMessage = "The connectionInfo parameter is required to test the connection.";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                if (!taxSetting.IsEnabled)
                {
                    const string errorMessage = "Tax calculation disabled, enable before testing connection.";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                if (!taxSetting.IsValid)
                {
                    const string errorMessage = "AvaTax credentials are not provided.";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                var avaTaxClient = _avaTaxClientFactory(taxSetting);

                PingResultModel result;
                try
                {
                    result = avaTaxClient.Ping();
                }
                catch (JsonException e)
                {
                    var errorMessage = $"Avalara API service responded with some unexpected data. Please verify the link to Avalara API service.\nInner error: {e.Message}";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage, e);
                }
                catch (Exception e)
                {
                    var errorMessage = $"Failed to connect to the Avalara API due to error: {e.Message}";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage, e);
                }

                if (result.authenticated != true)
                {
                    var errorMessage = "Provided Avalara credentials are invalid. Please verify the account number and the license key.";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                retVal = Ok(result);
            })
            .OnError(_logger, AvalaraLogger.EventCodes.TaxPingError)
            .OnSuccess(_logger, AvalaraLogger.EventCodes.Ping);

            return retVal;
        }

        [HttpPost]
        [ResponseType(typeof(OrdersSynchronizationPushNotification))]
        [Route("orders/synchronize")]
        [CheckPermission(Permission = ModuleConstants.Permissions.TaxManage)]
        public IHttpActionResult SynchronizeOrders(OrdersSynchronizationRequest request)
        {
            var notification = Enqueue(request);
            _pushNotificationManager.Upsert(notification);
            return Ok(notification);
        }

        [HttpPost]
        [ResponseType(typeof(void))]
        [Route("orders/{jobId}/cancel")]
        [CheckPermission(Permission = ModuleConstants.Permissions.TaxManage)]
        public IHttpActionResult CancelOrdersSynchronization(string jobId)
        {
            BackgroundJob.Delete(jobId);
            return Ok();
        }

        [HttpPost]
        [ResponseType(typeof(AddressValidationResult))]
        [Route("address/validate")]
        public IHttpActionResult ValidateAddress(AddressValidationRequest request)
        {
            var result = _addressValidationService.ValidateAddress(request.Address, request.StoreId);
            return Ok(result);
        }

        [HttpGet]
        [ResponseType(typeof(AvaTaxOrderSynchronizationStatus))]
        [Route("orders/{orderId}/status")]
        [CheckPermission(Permission = ModuleConstants.Permissions.TaxManage)]
        public async Task<IHttpActionResult> GetOrderSynchronizationStatus(string orderId)
        {
            var result = await _ordersSynchronizationService.GetOrderSynchronizationStatusAsync(orderId);
            return Ok(result);
        }

        private OrdersSynchronizationPushNotification Enqueue(OrdersSynchronizationRequest request)
        {
            var notification = new OrdersSynchronizationPushNotification(_userNameResolver.GetCurrentUserName())
            {
                Title = "Sending orders to AvaTax",
                Description = "Starting process..."
            };
            _pushNotificationManager.Upsert(notification);

            var ordersFeed = new FixedOrdersFeed(request.OrderIds, _orderService, _storeService);
            var jobId = BackgroundJob.Enqueue<OrdersSynchronizationJob>(x => x.Run(ordersFeed, notification, JobCancellationToken.Null, null));
            notification.JobId = jobId;

            return notification;
        }
    }
}
