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
    public class TaxTypeAdjustmentServiceTests
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
        private const string ShipmentMethodCode = "TestShippingMethod";
        private const string ShipmentTaxType = "FR012345";

        private readonly Mock<IStoreService> _storeService;

        public TaxTypeAdjustmentServiceTests()
        {
            _storeService = new Mock<IStoreService>();
        }

        [Fact]
        public void TestFillingTaxTypeForShipment()
        {
            // Arrange
            var order = new CustomerOrder
            {
                StoreId = TestStoreId,
                Shipments = new List<Shipment>
                {
                    new Shipment
                    {
                        ShipmentMethodCode = ShipmentMethodCode
                    }
                }
            };

            var store = new Store
            {
                Id = TestStoreId,
                ShippingMethods = new List<ShippingMethod>
                {
                    new TestShippingMethod(ShipmentMethodCode)
                    {
                        TaxType = ShipmentTaxType
                    }
                }
            };

            _storeService.Setup(x => x.GetByIds(new[] {TestStoreId})).Returns(new[] { store });

            var target = new TaxTypeAdjustmentService(_storeService.Object);

            // Act
            target.AdjustTaxTypesFor(order);

            // Assert
            foreach (var shipment in order.Shipments)
            {
                Assert.Equal(ShipmentTaxType, shipment.TaxType);;
            }
        }
    }
}