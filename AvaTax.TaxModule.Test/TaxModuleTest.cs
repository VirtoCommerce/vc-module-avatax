using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Results;
using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Web;
using AvaTax.TaxModule.Web.Controller;
using AvaTax.TaxModule.Web.Services;
using Common.Logging;
using Moq;
using VirtoCommerce.Domain.Customer.Model;
using VirtoCommerce.Domain.Customer.Services;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;
using Address = VirtoCommerce.Domain.Commerce.Model.Address;
using AddressType = VirtoCommerce.Domain.Commerce.Model.AddressType;
using LineItem = VirtoCommerce.Domain.Order.Model.LineItem;
using Shipment = VirtoCommerce.Domain.Order.Model.Shipment;

namespace AvaTax.TaxModule.Test
{
    public class TaxModuleTest
    {
        private static readonly List<SettingEntry> _settings = new List<SettingEntry>
        {
            new SettingEntry
            {
                Value = AvalaraUsername,
                Name = UsernamePropertyName,
                ValueType = SettingValueType.ShortText
            },
            new SettingEntry
            {
                Value = AvalaraPassword,
                Name = PasswordPropertyName,
                ValueType = SettingValueType.SecureString
            },
            new SettingEntry
            {
                Value = AvalaraServiceUrl,
                Name = ServiceUrlPropertyName,
                ValueType = SettingValueType.ShortText
            },
            new SettingEntry
            {
                Value = AvalaraCompanyCode,
                Name = CompanyCodePropertyName,
                ValueType = SettingValueType.ShortText
            },
            new SettingEntry
            {
                Value = "True",
                Name = IsEnabledPropertyName,
                ValueType = SettingValueType.Boolean
            }
        };

        private readonly AvaTaxController _controller;

        private const string AvalaraUsername = "1100165101";
        private const string AvalaraPassword = "AE5F97FA88A8D87D";
        private const string AvalaraServiceUrl = "https://sandbox-rest.avatax.com";
        private const string AvalaraCompanyCode = "APITrialCompany";

        const string UsernamePropertyName = "Avalara.Tax.Credentials.AccountNumber";
        const string PasswordPropertyName = "Avalara.Tax.Credentials.LicenseKey";
        const string ServiceUrlPropertyName = "Avalara.Tax.Credentials.ServiceUrl";
        const string CompanyCodePropertyName = "Avalara.Tax.Credentials.CompanyCode";
        const string IsEnabledPropertyName = "Avalara.Tax.IsEnabled";

        private const string ApplicationName = "AvaTax.TaxModule for VirtoCommerce";
        private const string ApplicationVersion = "2.x";

        public TaxModuleTest()
        {
            _controller = GetTaxController();
        }

        [Fact]
        public void Valid_address_successfull_validation()
        {
            //arrange
            var validTestAddress = GetValidAddress();
            //act
            var response = _controller.ValidateAddress(validTestAddress);

            //assert
            Assert.IsType<OkNegotiatedContentResult<AddressResolutionModel>>(response);
        }

        [Fact]
        public void Invalid_address_error_validation()
        {
            //arrange
            var invalidTestAddress = GetInvalidAddress();

            //act
            var response = _controller.ValidateAddress(invalidTestAddress);

            //assert
            Assert.IsType<BadRequestErrorMessageResult>(response);
        }

        [Fact]
        public void Valid_evaluation_context_successfull_tax_calculation()
        {
            //arrange
            var memberService = new Mock<IMemberService>();
            memberService.Setup(s => s.GetByIds(It.IsAny<string[]>(), null, null))
                .Returns<string[], string, string[]>((ids, responseGroup, memberTypes) => new Member[] { new Contact() });

            var logService = new Mock<ILog>();

            var avaTaxRateProvider = new AvaTaxRateProvider(memberService.Object, logService.Object, CreateAvaTaxClient, _settings.ToArray());

            var context = new TaxEvaluationContext
            {
                Address = GetValidAddress(),
                Customer = new Contact { Id = Guid.NewGuid().ToString() },
                Lines = GetContextTaxLines(),
                Currency = "USD",
                Id = Guid.NewGuid().ToString(),
                Type = "cart"
            };

            //act
            var rates = avaTaxRateProvider.CalculateRates(context);

            //assert
            Assert.NotEmpty(rates);
        }

        private AvaTaxClient CreateAvaTaxClient()
        {
            var machineName = Environment.MachineName;
            var avaTaxUri = new Uri(AvalaraServiceUrl);
            var result = new AvaTaxClient(ApplicationName, ApplicationVersion, machineName, avaTaxUri)
                .WithSecurity(AvalaraUsername, AvalaraPassword);

            return result;
        }

        private static Address GetValidAddress()
        {
            return new Address
            {
                AddressType = AddressType.Shipping,
                Phone = "+68787687",
                PostalCode = "19142",
                CountryCode = "US",
                CountryName = "United states",
                Email = "user@mail.com",
                FirstName = "first name",
                LastName = "last name",
                Line1 = "6025 Greenway Ave",
                City = "Philadelphia",
                RegionId = "PA",
                RegionName = "Pennsylvania",
                Organization = "org1"
            };
        }

        private static Address GetInvalidAddress()
        {
            return new Address
            {
                AddressType = AddressType.Shipping,
                Phone = "+0000000",
                PostalCode = "10000",
                CountryCode = "US",
                CountryName = "United states",
                Email = "user@mail.com",
                FirstName = "first name",
                LastName = "last name",
                Line1 = "11111 Bad street address",
                City = "New York",
                RegionId = "CA",
                RegionName = "California",
                Organization = "org1"
            };
        }

        private static ICollection<TaxLine> GetContextTaxLines()
        {

            return new[] 
            {
                new TaxLine
                {
                    Id = Guid.NewGuid().ToString(),
                    Price = 20,
                    Name = "shoes",
                    Code = "SKU1",
                    Amount = 1
                },
                new TaxLine
                {
                    Id = Guid.NewGuid().ToString(),
                    Price = 100,
                    Name = "t-shirt",
                    Code = "SKU2",
                    Amount = 1
                }
            };
        }

        private static CustomerOrder GetTestOrder(string id)
        {
            var order = new CustomerOrder
            {
                Id = id,
                Currency = "USD",
                CustomerId = "Test Customer",
                EmployeeId = "employee",
                StoreId = "test store",
                CreatedDate = DateTime.Now,
                Addresses = new[]
                {
                    new Address
                    {
                        AddressType = AddressType.Shipping,
                        Phone = "+68787687",
                        PostalCode = "19142",
                        CountryCode = "US",
                        CountryName = "United states",
                        Email = "user@mail.com",
                        FirstName = "first name",
                        LastName = "last name",
                        Line1 = "6025 Greenway Ave",
                        City = "Philadelphia",
                        RegionId = "PA",
                        RegionName = "Pennsylvania",
                        Organization = "org1"
                    }
                }.ToList(),
                Items = new List<LineItem>
                {
                    new LineItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Price = 20,
                        ProductId = "shoes",
                        CatalogId = "catalog",
                        Currency = "USD",
                        CategoryId = "category",
                        Name = "shoes",
                        Quantity = 2,
                        ShippingMethodCode = "EMS",
                    },
                    new LineItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Price = 100,
                        ProductId = "t-shirt",
                        CatalogId = "catalog",
                        CategoryId = "category",
                        Currency = "USD",
                        Name = "t-shirt",
                        Quantity = 2,
                        ShippingMethodCode = "EMS",
                    }
                },
                Shipments = new List<Shipment>
                {
                    new Shipment
                    {
                        Id = Guid.NewGuid().ToString(),
                        Currency = "USD",
                        DeliveryAddress = new Address
                        {
                            City = "london",
                            CountryName = "England",
                            Phone = "+68787687",
                            PostalCode = "2222",
                            CountryCode = "ENG",
                            Email = "user@mail.com",
                            FirstName = "first name",
                            LastName = "last name",
                            Line1 = "line 1",
                            Organization = "org1"
                        },
                    }
                },
                InPayments = new List<PaymentIn>
                {
                    new PaymentIn
                    {
                        Id = Guid.NewGuid().ToString(),
                        GatewayCode = "PayPal",
                        Currency = "USD",
                        Sum = 10,
                        CustomerId = "et",
                        IsApproved = false
                    }
                },
            };

            return order;
        }

        private AvaTaxController GetTaxController()
        {
            const string _isValidateAddressPropertyName = "Avalara.Tax.IsValidateAddress";

            var settingsManager = new Mock<ISettingsManager>();

            settingsManager.Setup(manager => manager.GetValue(UsernamePropertyName, string.Empty)).Returns(() => _settings.First(x => x.Name == UsernamePropertyName).Value);
            settingsManager.Setup(manager => manager.GetValue(PasswordPropertyName, string.Empty)).Returns(() => _settings.First(x => x.Name == PasswordPropertyName).Value);
            settingsManager.Setup(manager => manager.GetValue(ServiceUrlPropertyName, string.Empty)).Returns(() => _settings.First(x => x.Name == ServiceUrlPropertyName).Value);
            settingsManager.Setup(manager => manager.GetValue(CompanyCodePropertyName, string.Empty)).Returns(() => _settings.First(x => x.Name == CompanyCodePropertyName).Value);
            settingsManager.Setup(manager => manager.GetValue(IsEnabledPropertyName, true)).Returns(() => true);
            settingsManager.Setup(manager => manager.GetValue(_isValidateAddressPropertyName, true)).Returns(() => true);

            var avalaraTax = new AvaTaxSettings(UsernamePropertyName, PasswordPropertyName, ServiceUrlPropertyName, CompanyCodePropertyName, IsEnabledPropertyName, _isValidateAddressPropertyName, settingsManager.Object);
            var logger = new Mock<ILog>();

            var controller = new AvaTaxController(avalaraTax, logger.Object, CreateAvaTaxClient);
            return controller;
        }
    }
}
