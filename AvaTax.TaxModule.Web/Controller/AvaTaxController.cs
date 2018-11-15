using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Data.Logging;
using AvaTax.TaxModule.Web.Services;
using Common.Logging;
using Newtonsoft.Json;
using System;
using System.Web.Http;
using System.Web.Http.Description;

namespace AvaTax.TaxModule.Web.Controller
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [RoutePrefix("api/tax/avatax")]
    public class AvaTaxController : ApiController
    {
        private readonly Func<IAvaTaxSettings, AvaTaxClient> _avaTaxClientFactory;
        private readonly AvalaraLogger _logger;

        [CLSCompliant(false)]
        public AvaTaxController(ILog log, Func<IAvaTaxSettings, AvaTaxClient> avaTaxClientFactory)
        {
            _logger = new AvalaraLogger(log);
            _avaTaxClientFactory = avaTaxClientFactory;
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
    }
}
