using System;
using System.Collections.Generic;
using System.Linq;
using AvaTax.TaxModule.Data.Services;
using Moq;
using VirtoCommerce.Domain.Common;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Shipping.Model;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Domain.Store.Services;
using Xunit;

namespace AvaTax.TaxModule.Test
{
    [CLSCompliant(false)]
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

        public OrderTaxTypeResolverTests()
        {
            _storeService = new Mock<IStoreService>();
        }

        [Theory]
        [InlineData(null, TestTaxType, TestTaxType)]
        [InlineData(TestTaxType, null, TestTaxType)]
        public void TestFillingTaxTypeForShipment(string shipmentTaxType, string shippingMethodTaxType, string expectedTaxType)
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
                ShippingMethods = new List<ShippingMethod>
                {
                    new TestShippingMethod(TestShipmentMethodCode)
                    {
                        TaxType = shippingMethodTaxType
                    }
                }
            };

            _storeService.Setup(x => x.GetByIds(new[] {TestStoreId})).Returns(new[] { store });

            var target = new OrderTaxTypeResolver(_storeService.Object);

            // Act
            target.ResolveTaxTypeForOrder(order);

            // Assert
            foreach (var shipment in order.Shipments)
            {
                Assert.Equal(expectedTaxType, shipment.TaxType);;
            }
        }
    }
}