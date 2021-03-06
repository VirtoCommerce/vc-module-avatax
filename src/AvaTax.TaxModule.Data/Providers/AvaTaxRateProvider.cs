using System;
using System.Collections.Generic;
using System.Linq;
using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Core;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data.Logging;
using AvaTax.TaxModule.Data.Model;
using AvaTax.TaxModule.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.TaxModule.Core.Model;

namespace AvaTax.TaxModule.Data.Providers
{
    public class AvaTaxRateProvider : TaxProvider
    {
        private readonly AvalaraLogger _logger;
        private readonly Func<IAvaTaxSettings, AvaTaxClient> _avaTaxClientFactory;
        private readonly AvaTaxSecureOptions _options;

        public AvaTaxRateProvider() 
        {
            Code = nameof(AvaTaxRateProvider);
        }

        public AvaTaxRateProvider(ILogger<AvaTaxRateProvider> log, Func<IAvaTaxSettings, AvaTaxClient> avaTaxClientFactory, IOptions<AvaTaxSecureOptions> options)
            : this()
        {
            _logger = new AvalaraLogger(log);
            _avaTaxClientFactory = avaTaxClientFactory;
            _options = options.Value;
        }

        public override IEnumerable<TaxRate> CalculateRates(IEvaluationContext context)
        {
            var taxEvalContext = context as TaxEvaluationContext;
            if (taxEvalContext == null)
            {
                throw new ArgumentException("Given context is not an instance of the TaxEvaluationContext class.", nameof(context));
            }

            var retVal = GetTaxRates(taxEvalContext);
            return retVal;
        }

        protected virtual List<TaxRate> GetTaxRates(TaxEvaluationContext evalContext)
        {
            var retVal = new List<TaxRate>();
            LogInvoker<AvalaraLogger.TaxRequestContext>.Execute(log =>
            {
                var avaSettings = AvaTaxSettings.FromSettings(Settings, _options);
                Validate(avaSettings);

                var companyCode = avaSettings.CompanyCode;
                var createTransactionModel = AbstractTypeFactory<AvaCreateTransactionModel>.TryCreateInstance();
                createTransactionModel.FromContext(evalContext, companyCode);
                createTransactionModel.commit = false;

                log.docCode = createTransactionModel.code;
                log.docType = createTransactionModel.type.ToString();
                log.customerCode = createTransactionModel.customerCode;
                if (createTransactionModel.IsValid)
                {
                    var avaTaxClient = _avaTaxClientFactory(avaSettings);
                    var transaction = avaTaxClient.CreateTransaction(string.Empty, createTransactionModel);

                    if (!transaction.lines.IsNullOrEmpty())
                    {
                        foreach (var taxLine in transaction.lines)
                        {
                            var rate = new TaxRate
                            {
                                Rate = taxLine.tax ?? 0.0m,
                                Currency = evalContext.Currency,
                                TaxProvider = this,
                                Line = evalContext.Lines.FirstOrDefault(x => x.Id == taxLine.lineNumber)
                            };
                            if (rate.Line != null)
                            {
                                retVal.Add(rate);
                            }
                        }
                    }
                }

            })
            .OnError(_logger, AvalaraLogger.EventCodes.TaxCalculationError)
            .OnSuccess(_logger, AvalaraLogger.EventCodes.GetSalesInvoiceRequestTime);

            return retVal;
        }

        protected virtual void Validate(IAvaTaxSettings settings)
        {
            if (!settings.IsValid)
            {
                throw new Exception("Tax credentials not provided");
            }
        }
    }
}
