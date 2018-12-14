using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Data.Model;
using AvaTax.TaxModule.Data.Logging;
using AvaTax.TaxModule.Web.Services;
using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using AvaTax.TaxModule.Core.Services;
using VirtoCommerce.Domain.Common;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Data
{
    public class AvaTaxRateProvider : TaxProvider
    {
        private readonly AvalaraLogger _logger;
        private readonly Func<IAvaTaxSettings, AvaTaxClient> _avaTaxClientFactory;

        public AvaTaxRateProvider()
            : base("AvaTaxRateProvider")
        {
        }

        [CLSCompliant(false)]
        public AvaTaxRateProvider(ILog log, Func<IAvaTaxSettings, AvaTaxClient> avaTaxClientFactory, params SettingEntry[] settings)
            : this()
        {
            Settings = settings;
            _logger = new AvalaraLogger(log);
            _avaTaxClientFactory = avaTaxClientFactory;
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
            List<TaxRate> retVal = new List<TaxRate>();
            LogInvoker<AvalaraLogger.TaxRequestContext>.Execute(log =>
            {
                var avaSettings = AvaTaxSettings.FromSettings(Settings);
                Validate(avaSettings);

                //Evaluate taxes only for cart to preventing registration redundant transactions in avalara
                var createTransactionModel = AbstractTypeFactory<AvaCreateTransactionModel>.TryCreateInstance();
                createTransactionModel.FromContext(evalContext);
                createTransactionModel.companyCode = avaSettings.CompanyCode;
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
            if (!settings.IsEnabled || !settings.IsValid)
            {
                throw new Exception("Tax calculation disabled or credentials not provided");
            }
        }
    }
}