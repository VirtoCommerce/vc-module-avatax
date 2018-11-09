using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Web.Converters;
using AvaTax.TaxModule.Web.Logging;
using AvaTax.TaxModule.Web.Services;
using Common.Logging;
using domainModel = VirtoCommerce.Domain.Commerce.Model;

namespace AvaTax.TaxModule.Web.Controller
{
	[ApiExplorerSettings(IgnoreApi = true)]
    [RoutePrefix("api/tax/avatax")]
    public class AvaTaxController : ApiController
	{
	    private readonly Func<AvaTaxClient> _avaTaxClientFactory;
        private readonly ITaxSettings _taxSettings;
        private readonly AvalaraLogger _logger;

        [CLSCompliant(false)]
        public AvaTaxController(ITaxSettings taxSettings, ILog log, Func<AvaTaxClient> avaTaxClientFactory)
        {
            _taxSettings = taxSettings;
            _logger = new AvalaraLogger(log);
            _avaTaxClientFactory = avaTaxClientFactory;
        }

        [HttpGet]
        [ResponseType(typeof(void))]
        [Route("ping")]
        public IHttpActionResult TestConnection()
        {
            IHttpActionResult retVal = BadRequest();
            LogInvoker<AvalaraLogger.TaxRequestContext>.Execute(log =>
            {
                if (string.IsNullOrEmpty(_taxSettings.Username)
                    || string.IsNullOrEmpty(_taxSettings.Password)
                    || string.IsNullOrEmpty(_taxSettings.ServiceUrl)
                    || string.IsNullOrEmpty(_taxSettings.CompanyCode))
                {
                    const string errorMessage = "AvaTax credentials not provided";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                if (!_taxSettings.IsEnabled)
                {
                    const string errorMessage = "Tax calculation disabled, enable before testing connection";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                var avaTaxClient = _avaTaxClientFactory();

                var result = avaTaxClient.Ping();
                // TODO: error handling?

                //var taxSvc = new JsonTaxSvc(_taxSettings.Username, _taxSettings.Password, _taxSettings.ServiceUrl);
                //var result = taxSvc.Ping();
                //if (!result.ResultCode.Equals(SeverityLevel.Success))
                //{
                //    retVal =
                //        BadRequest(string.Join(Environment.NewLine,
                //            result.Messages.Where(ms => ms.Severity == SeverityLevel.Error).Select(
                //        m => m.Summary + string.Format(" [{0} - {1}] ", m.RefersTo, m.Details == null ? string.Empty : string.Join(", ", m.Details)))));
                //    throw new Exception((retVal as BadRequestErrorMessageResult).Message);
                //}

                retVal = Ok(result);
            })
            .OnError(_logger, AvalaraLogger.EventCodes.TaxPingError)
            .OnSuccess(_logger, AvalaraLogger.EventCodes.Ping);

            return retVal;
        }

        [HttpPost]
        [ResponseType(typeof(bool))]
        [Route("address")]
        public IHttpActionResult ValidateAddress(domainModel.Address address)
        {
            IHttpActionResult retVal = BadRequest();
            LogInvoker<AvalaraLogger.TaxRequestContext>.Execute(log =>
            {
                if (!_taxSettings.IsValidateAddress)
                {
                    const string errorMessage = "AvaTax address validation is disabled.";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                if (string.IsNullOrEmpty(_taxSettings.Username)
                    || string.IsNullOrEmpty(_taxSettings.Password)
                    || string.IsNullOrEmpty(_taxSettings.ServiceUrl)
                    || string.IsNullOrEmpty(_taxSettings.CompanyCode))
                {
                    const string errorMessage = "AvaTax credentials are not provided.";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                var avaTaxClient = _avaTaxClientFactory();
                var addressValidationInfo = address.ToAddressValidationInfo();

                var addressResolutionModel = avaTaxClient.ResolveAddressPost(addressValidationInfo);
                
                // If the address cannot be resolved, it's location will be null.
                // This might mean that the address is invalid.
                if (addressResolutionModel.coordinates == null)
                {
                    var resolutionMessages = addressResolutionModel.messages ?? new List<AvaTaxMessage>();
                    var messageStrings = resolutionMessages.Select(x => $"{x.severity}: {x.summary} [{x.refersTo} - {x.details}]");
                    var errorMessage = string.Join(Environment.NewLine, messageStrings);

                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                retVal = Ok(addressResolutionModel);
            })
            .OnError(_logger, AvalaraLogger.EventCodes.AddressValidationError)
            .OnSuccess(_logger, AvalaraLogger.EventCodes.ValidateAddress);

            return retVal;
        }
    }
}
