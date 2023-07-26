using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AvaTax.TaxModule.Data.Services;
using Moq;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Model.Search;
using VirtoCommerce.ShippingModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;

namespace AvaTax.TaxModule.Test
{
    public class OrderTaxTypeResolverTests
    {
        private class TestShippingMethod : ShippingMethod
        {
            public TestShippingMethod(string code)
                : base(code)
            {
            }

            public override IEnumerable<ShippingRate> CalculateRates(IEvaluationContext context)
            {
                return Enumerable.Empty<ShippingRate>();
            }
        }

        private const string TestStoreId = "TestStore";
        private const string TestShipmentMethodCode = "TestShippingMethod";
        private const string TestTaxType = "FR012345";

        private readonly Mock<IStoreService> _storeService;
        private readonly Mock<IShippingMethodsSearchService> _shippingMethodsSearchService;

        public OrderTaxTypeResolverTests()
        {
            _storeService = new Mock<IStoreService>();
            _shippingMethodsSearchService = new Mock<IShippingMethodsSearchService>();
        }

        [Theory]
        [InlineData(null, TestTaxType, TestTaxType)]
        [InlineData(TestTaxType, null, TestTaxType)]
        public async Task TestFillingTaxTypeForShipment(string shipmentTaxType, string shippingMethodTaxType, string expectedTaxType)
        {
            // Arrange
            var order = new CustomerOrder
            {
                StoreId = TestStoreId,
                Shipments = new List<Shipment>
                {
                    new Shipment
                    {
                        ShipmentMethodCode = TestShipmentMethodCode,
                        TaxType = shipmentTaxType
                    }
                }
            };

            var store = new Store
            {
                Id = TestStoreId,
            };


            var shippingMethods = new List<ShippingMethod>
            {
                new TestShippingMethod(TestShipmentMethodCode)
                {
                    TaxType = shippingMethodTaxType
                }
            };

            var searchCriteria = new ShippingMethodsSearchCriteria
            {
                IsActive = true,
                Keyword = TestShipmentMethodCode
            };

            _shippingMethodsSearchService
                .Setup(x => x.SearchAsync(searchCriteria, It.IsAny<bool>()))
                .ReturnsAsync(() => new ShippingMethodsSearchResult
                {
                    TotalCount = 1,
                    Results = shippingMethods
                });

            _storeService
                .Setup(x => x.GetAsync(new[] { TestStoreId }, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new[] { store });

            var target = new OrderTaxTypeResolver(_storeService.Object, _shippingMethodsSearchService.Object);

            // Act
            await target.ResolveTaxTypeForOrderAsync(order);

            // Assert
            foreach (var shipment in order.Shipments)
            {
                Assert.Equal(expectedTaxType, shipment.TaxType);
            }
        }
    }
}
