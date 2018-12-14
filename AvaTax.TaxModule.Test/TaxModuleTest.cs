using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Data;
using AvaTax.TaxModule.Web.Services;
using Common.Logging;
using Moq;
using System;
using System.Collections.Generic;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data.Services;
using VirtoCommerce.Domain.Customer.Model;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;
using Address = VirtoCommerce.Domain.Commerce.Model.Address;
using AddressType = VirtoCommerce.Domain.Commerce.Model.AddressType;

namespace AvaTax.TaxModule.Test
{
    [Trait("Category", "CI")]
    public class TaxModuleTest
    {
        private const string AvalaraUsername = "1100165101";
        private const string AvalaraPassword = "AE5F97FA88A8D87D";
        private const string AvalaraServiceUrl = "https://sandbox-rest.avatax.com";
        private const string AvalaraCompanyCode = "APITrialCompany";

        private const string UsernamePropertyName = "Avalara.Tax.Credentials.AccountNumber";
        private const string PasswordPropertyName = "Avalara.Tax.Credentials.LicenseKey";
        private const string ServiceUrlPropertyName = "Avalara.Tax.Credentials.ServiceUrl";
        private const string CompanyCodePropertyName = "Avalara.Tax.Credentials.CompanyCode";
        private const string IsEnabledPropertyName = "Avalara.Tax.IsEnabled";

        private const string ApplicationName = "AvaTax.TaxModule for VirtoCommerce";
        private const string ApplicationVersion = "2.x";

        private static readonly List<SettingEntry> Settings = new List<SettingEntry>
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

        public static readonly IEnumerable<object[]> TestData = new List<object[]>
        {
            new object[] {GetValidAddress(), true},
            new object[] {GetInvalidAddress(), false},
            new object[] {GetEmptyAddress(), false}
        };

        [Theory]
        [MemberData(nameof(TestData))]
        [CLSCompliant(false)]
        public void TestAddressValidation(Address address, bool expectedIsValid)
        {
            const string storeId = "some-test-store";

            // Arrange
            var storeService = new Mock<IStoreService>();
            storeService.Setup(x => x.GetById(storeId)).Returns(new Store
            {
                TaxProviders = new List<TaxProvider>
                {
                    new AvaTaxRateProvider
                    {
                        IsActive = true,
                        Settings = Settings
                    }
                }
            });

            var target = new AddressValidationService(storeService.Object, CreateAvaTaxClient);

            // Act
            var result = target.ValidateAddress(address, storeId);

            // Assert
            Assert.Equal(expectedIsValid, result.AddressIsValid);
        }

        [Fact]
        public void Valid_evaluation_context_successfull_tax_calculation()
        {
            //arrange
            var logService = new Mock<ILog>();
            var avaTaxRateProvider = new AvaTaxRateProvider(logService.Object, CreateAvaTaxClient, Settings.ToArray());

            var context = new TaxEvaluationContext
            {
                Address = GetValidAddress(),
                Customer = new Contact { Id = Guid.NewGuid().ToString() },
                Lines = GetContextTaxLines(),
                Currency = "USD",
                Id = Guid.NewGuid().ToString()
            };

            //act
            var rates = avaTaxRateProvider.CalculateRates(context);

            //assert
            Assert.NotEmpty(rates);
        }

        private static AvaTaxClient CreateAvaTaxClient(IAvaTaxSettings settings)
        {
            var machineName = Environment.MachineName;
            var avaTaxUri = new Uri(settings.ServiceUrl);
            var result = new AvaTaxClient(ApplicationName, ApplicationVersion, machineName, avaTaxUri)
                .WithSecurity(settings.AccountNumber, settings.LicenseKey);

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

        private static Address GetEmptyAddress()
        {
            return new Address();
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
    }
}
