using AvaTax.TaxModule.Web.Converters;
using AvaTax.TaxModule.Web.Logging;
using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Common;
using VirtoCommerce.Domain.Customer.Model;
using VirtoCommerce.Domain.Customer.Services;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Web
{
    public class AvaTaxRateProvider : TaxProvider
    {
        #region const

        private const string accountNumberPropertyName = "Avalara.Tax.Credentials.AccountNumber";
        private const string licenseKeyPropertyName = "Avalara.Tax.Credentials.LicenseKey";
        private const string serviceUrlPropertyName = "Avalara.Tax.Credentials.ServiceUrl";
        private const string companyCodePropertyName = "Avalara.Tax.Credentials.CompanyCode";
        private const string isEnabledPropertyName = "Avalara.Tax.IsEnabled";
        private const string isValidateAddressPropertyName = "Avalara.Tax.IsValidateAddress";

        #endregion

        private readonly AvalaraLogger _logger;
        private readonly IMemberService _memberService;
        private readonly Func<AvaTaxClient> _avaTaxClientFactory;

        public AvaTaxRateProvider()
            : base("AvaTaxRateProvider")
        {
        }

        public AvaTaxRateProvider(IMemberService memberService, ILog log, Func<AvaTaxClient> avaTaxClientFactory, params SettingEntry[] settings)
            : this()
        {
            Settings = settings;
            _logger = new AvalaraLogger(log);
            _memberService = memberService;
            _avaTaxClientFactory = avaTaxClientFactory;
        }

        private int AccountNumber => int.Parse(GetSetting(accountNumberPropertyName));

        private string LicenseKey => GetSetting(licenseKeyPropertyName);

        private string CompanyCode => GetSetting(companyCodePropertyName);

        private string ServiceUrl => GetSetting(serviceUrlPropertyName);

        private bool IsEnabled => bool.Parse(GetSetting(isEnabledPropertyName));

        private bool IsValidateAddressEnabled => bool.Parse(GetSetting(isValidateAddressPropertyName));

        public override IEnumerable<TaxRate> CalculateRates(IEvaluationContext context)
        {
            var taxEvalContext = context as TaxEvaluationContext;
            if (taxEvalContext == null)
            {
                throw new NullReferenceException("taxEvalContext");
            }

            var retVal = GetTaxRates(taxEvalContext);
            return retVal;
        }


        public virtual void CalculateOrderTax(CustomerOrder order)
        {
            LogInvoker<AvalaraLogger.TaxRequestContext>.Execute(log =>
            {
                Validate();

                Member member = null;
                if (order.CustomerId != null)
                    member = _memberService.GetByIds(new[] { order.CustomerId }).FirstOrDefault();

                //if all payments completed commit tax document in avalara
                var isCommit = order.InPayments != null && order.InPayments.Any() && order.InPayments.All(pi => pi.IsApproved);

                //update transaction in avalara
                var createTransactionModel = order.ToAvaTaxCreateTransactionModel(CompanyCode, member, DocumentType.SalesInvoice, isCommit);
                if (createTransactionModel != null)
                {
                    log.docCode = createTransactionModel.code;
                    log.docType = createTransactionModel.type.ToString();
                    log.customerCode = createTransactionModel.customerCode;
                    log.amount = (double)order.Sum;
                    log.isCommit = isCommit;

                    var avaTaxClient = _avaTaxClientFactory();
                    var transaction = avaTaxClient.CreateTransaction(string.Empty, createTransactionModel);
                    // TODO: error handling?
                }
            })
            .OnError(_logger, AvalaraLogger.EventCodes.TaxCalculationError)
            .OnSuccess(_logger, AvalaraLogger.EventCodes.GetSalesInvoiceRequestTime);
        }

        public virtual void AdjustOrderTax(CustomerOrder originalOrder, CustomerOrder modifiedOrder)
        {
            LogInvoker<AvalaraLogger.TaxRequestContext>.Execute(log =>
            {
                Validate();

                //if all payments completed commit tax document in avalara
                var isCommit = modifiedOrder.InPayments != null && modifiedOrder.InPayments.Any()
                    && modifiedOrder.InPayments.All(pi => pi.IsApproved);

                Member member = null;
                if (modifiedOrder.CustomerId != null)
                    member = _memberService.GetByIds(new[] { modifiedOrder.CustomerId }).FirstOrDefault();

                var transactionModel = modifiedOrder.ToAvaTaxCreateOrAdjustTransactionModel(originalOrder, CompanyCode,
                    member, DocumentType.ReturnInvoice, isCommit);
                if (transactionModel != null)
                {
                    log.docCode = transactionModel.createTransactionModel.referenceCode;
                    log.docType = transactionModel.createTransactionModel.type.ToString();
                    log.customerCode = transactionModel.createTransactionModel.customerCode;
                    log.amount = (double)originalOrder.Sum;

                    var avaTaxClient = _avaTaxClientFactory();
                    var transaction = avaTaxClient.CreateOrAdjustTransaction(string.Empty, transactionModel);
                    // TODO: handle errors?
                }
            })
            .OnError(_logger, AvalaraLogger.EventCodes.TaxCalculationError)
            .OnSuccess(_logger, AvalaraLogger.EventCodes.GetTaxRequestTime);
        }

        public virtual void CancelTaxDocument(CustomerOrder order)
        {
            LogInvoker<AvalaraLogger.TaxRequestContext>.Execute(log =>
            {
                Validate();

                var voidTransactionModel = order.ToAvaTaxVoidTransactionModel(VoidReasonCode.DocDeleted);
                if (voidTransactionModel != null)
                {
                    log.docCode = order.Number;
                    log.docType = DocumentType.Any.ToString();

                    var avaTaxClient = _avaTaxClientFactory();
                    var transaction = avaTaxClient.VoidTransaction(CompanyCode, order.Number, null, voidTransactionModel);
                    // TODO: error handling?
                }
            })
            .OnError(_logger, AvalaraLogger.EventCodes.TaxCalculationError)
            .OnSuccess(_logger, AvalaraLogger.EventCodes.GetTaxRequestTime);
        }


        protected virtual List<TaxRate> GetTaxRates(TaxEvaluationContext evalContext)
        {
            List<TaxRate> retVal = new List<TaxRate>();
            LogInvoker<AvalaraLogger.TaxRequestContext>.Execute(log =>
            {
                Validate();

                //Evaluate taxes only for cart to preventing registration redundant transactions in avalara
                var createTransactionModel = evalContext.ToAvaTaxCreateTransactionModel(CompanyCode, false);
                if (createTransactionModel != null)
                {
                    log.docCode = createTransactionModel.code;
                    log.docType = createTransactionModel.type.ToString();
                    log.customerCode = createTransactionModel.customerCode;

                    var avaTaxClient = _avaTaxClientFactory();
                    var transaction = avaTaxClient.CreateTransaction(string.Empty, createTransactionModel);
                    // TODO: error handling?

                    if (!transaction.lines.IsNullOrEmpty())
                    {
                        foreach (var taxLine in transaction.lines)
                        {
                            var rate = new TaxRate
                            {
                                Rate = taxLine.tax ?? 0.0m,
                                Currency = evalContext.Currency,
                                TaxProvider = this,
                                Line = evalContext.Lines.First(line => line.Id == taxLine.lineNumber)
                            };
                            retVal.Add(rate);
                        }
                    }
                }
            })
            .OnError(_logger, AvalaraLogger.EventCodes.TaxCalculationError)
            .OnSuccess(_logger, AvalaraLogger.EventCodes.GetSalesInvoiceRequestTime);

            return retVal;
        }

        protected virtual void Validate()
        {
            if (!IsEnabled || AccountNumber == 0
                    || string.IsNullOrEmpty(LicenseKey)
                    || string.IsNullOrEmpty(ServiceUrl)
                    || string.IsNullOrEmpty(CompanyCode))
            {
                throw new Exception("Tax calculation disabled or credentials not provided");
            }
        }
    }
}