using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Core;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data.Providers;
using AvaTax.TaxModule.Data.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.TaxModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Model.Search;
using VirtoCommerce.TaxModule.Core.Services;
using Xunit;
using Address = VirtoCommerce.TaxModule.Core.Model.Address;
using Store = VirtoCommerce.StoreModule.Core.Model.Store;

namespace AvaTax.TaxModule.Test
{
    [Trait("Category", "CI")]
    public class TaxModuleTest
    {
        private const string AvalaraUsername = "1100165101";
        private const string AvalaraPassword = "AE5F97FA88A8D87D";
        private const string AvalaraServiceUrl = "https://sandbox-rest.avatax.com";
        private const string AvalaraCompanyCode = "APITrialCompany";

        private const string ApplicationName = "AvaTax.TaxModule for VirtoCommerce";
        private const string ApplicationVersion = "3.x";

        private AvaTaxSecureOptions Options = new AvaTaxSecureOptions
        {
            AccountNumber = AvalaraUsername,
            LicenseKey = AvalaraPassword
        };

        private static readonly List<ObjectSettingEntry> Settings = new List<ObjectSettingEntry>
        {
            new ObjectSettingEntry
            {
                Value = AvalaraServiceUrl,
                Name = ModuleConstants.Settings.Credentials.ServiceUrl.Name,
                ValueType = SettingValueType.ShortText
            },
            new ObjectSettingEntry
            {
                Value = AvalaraCompanyCode,
                Name = ModuleConstants.Settings.Credentials.CompanyCode.Name,
                ValueType = SettingValueType.ShortText
            },
        };

        public static readonly IEnumerable<object[]> TestData = new List<object[]>
        {
            new object[] {GetValidAddress(), true},
            new object[] {GetInvalidAddress(), false},
            new object[] {GetEmptyAddress(), false}
        };

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task TestAddressValidation(Address address, bool expectedIsValid)
        {
            const string storeId = "some-test-store";

            // Arrange
            var storeService = new Mock<IStoreService>();
            var taxProviderSearchService = new Mock<ITaxProviderSearchService>();

            var taxProviderSearchCriteria = new TaxProviderSearchCriteria
            {
                StoreIds = new[] { storeId },
                Keyword = typeof(AvaTaxRateProvider).Name
            };

            taxProviderSearchService
                .Setup(x => x.SearchAsync(taxProviderSearchCriteria, It.IsAny<bool>()))
                .ReturnsAsync(new TaxProviderSearchResult
                {
                    TotalCount = 1,
                    Results = new List<TaxProvider>
                {
                    new AvaTaxRateProvider
                    {
                        IsActive = true,
                        Settings = Settings
                    }
                }
                });

            storeService
                .Setup(x => x.GetAsync(new[] { storeId }, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new[] { new Store { Id = storeId } });

            var options = new Mock<IOptions<AvaTaxSecureOptions>>();
            options.Setup(x => x.Value).Returns(Options);

            var target = new AddressValidationService(storeService.Object, CreateAvaTaxClient, taxProviderSearchService.Object, options.Object);

            // Act
            var result = await target.ValidateAddressAsync(address, storeId);

            // Assert
            Assert.Equal(expectedIsValid, result.AddressIsValid);
        }

        [Fact]
        public void Valid_evaluation_context_successfull_tax_calculation()
        {
            //arrange
            var logService = new Mock<ILogger<AvaTaxRateProvider>>();
            var options = new Mock<IOptions<AvaTaxSecureOptions>>();
            options.Setup(x => x.Value).Returns(Options);

            var avaTaxRateProvider = new AvaTaxRateProvider(logService.Object, CreateAvaTaxClient, options.Object);
            avaTaxRateProvider.Settings = Settings;

            var context = new TaxEvaluationContext
            {
                Address = GetValidAddress(),
                Customer = new Customer() { Id = Guid.NewGuid().ToString() },
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
