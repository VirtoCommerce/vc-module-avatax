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
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Customer.Model;
using VirtoCommerce.Domain.Customer.Services;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;
using Address = VirtoCommerce.Domain.Commerce.Model.Address;
using AddressType = VirtoCommerce.Domain.Commerce.Model.AddressType;
using CartAddress = VirtoCommerce.Domain.Commerce.Model.Address;
using CartAddressType = VirtoCommerce.Domain.Commerce.Model.AddressType;
using CartLineItem = VirtoCommerce.Domain.Cart.Model.LineItem;
using CartPayment = VirtoCommerce.Domain.Cart.Model.Payment;
using CartShipment = VirtoCommerce.Domain.Cart.Model.Shipment;
using coreTax = VirtoCommerce.Domain.Tax.Model;
using LineItem = VirtoCommerce.Domain.Order.Model.LineItem;
using Shipment = VirtoCommerce.Domain.Order.Model.Shipment;

namespace AvaTax.TaxModule.Test
{
    public class TaxModuleTest
    {
        private readonly AvaTaxController _controller;
        private readonly List<SettingEntry> _settings;

        string avalaraUsername = "1100165101";
        string avalaraPassword = "AE5F97FA88A8D87D";
        string avalaraServiceUrl = "https://sandbox-rest.avatax.com";
        string avalaraCompanyCode = "APITrialCompany";

        const string _usernamePropertyName = "Avalara.Tax.Credentials.AccountNumber";
        const string _passwordPropertyName = "Avalara.Tax.Credentials.LicenseKey";
        const string _serviceUrlPropertyName = "Avalara.Tax.Credentials.ServiceUrl";
        const string _companyCodePropertyName = "Avalara.Tax.Credentials.CompanyCode";
        const string _isEnabledPropertyName = "Avalara.Tax.IsEnabled";

        public TaxModuleTest()
        {
            _settings = new List<SettingEntry>
            {
                new SettingEntry
                {
                    Value = avalaraUsername,
                    Name = _usernamePropertyName,
                    ValueType = SettingValueType.ShortText
                },
                new SettingEntry
                {
                    Value = avalaraPassword,
                    Name = _passwordPropertyName,
                    ValueType = SettingValueType.SecureString
                },
                new SettingEntry
                {
                    Value = avalaraServiceUrl,
                    Name = _serviceUrlPropertyName,
                    ValueType = SettingValueType.ShortText
                },
                new SettingEntry
                {
                    Value = avalaraCompanyCode,
                    Name = _companyCodePropertyName,
                    ValueType = SettingValueType.ShortText
                },
                new SettingEntry { Value = "True", Name = _isEnabledPropertyName, ValueType = SettingValueType.Boolean }
            };

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

        [Fact(Skip = "AvaTaxRateProvider does not update carts anymore, so this test does not work.")]
        public void Valid_cart_successfull_tax_calculation()
        {
            //arrange
            var memberService = new Mock<IMemberService>();
            memberService.Setup(s => s.GetByIds(It.IsAny<string[]>(), null, null))
                .Returns<string[], string, string[]>((ids, responseGroup, memberTypes) => new Member[] { new Contact() });

            var logService = new Mock<ILog>();

            var avaTaxRateProvider = new AvaTaxRateProvider(memberService.Object, logService.Object, CreateAvaTaxClient, _settings.ToArray());
            var validCart = GetTestCart(Guid.NewGuid().ToString());

            //act
            avaTaxRateProvider.CalculateCartTax(validCart);

            //assert
            Assert.All(validCart.Items, item => Assert.True(item.TaxTotal > 0));
        }

        [Fact(Skip = "AvaTaxRateProvider does not update orders anymore, so this test does not work.")]
        public void Valid_order_successfull_tax_calculation()
        {
            //arrange
            var memberService = new Mock<IMemberService>();
            memberService.Setup(s => s.GetByIds(It.IsAny<string[]>(), null, null))
                .Returns<string[], string, string[]>((ids, responseGroup, memberTypes) => new Member[] { new Contact() });

            var logService = new Mock<ILog>();

            var avaTaxRateProvider = new AvaTaxRateProvider(memberService.Object, logService.Object, CreateAvaTaxClient, _settings.ToArray());
            var validOrder = GetTestOrder(Guid.NewGuid().ToString());

            //act
            avaTaxRateProvider.CalculateOrderTax(validOrder);

            //assert
            Assert.All(validOrder.Items, item => Assert.True(item.TaxTotal > 0));
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

        //[Fact]
        //public void Invalid_cart_address_error_tax_calculation()
        //{
        //    //arrange
        //    var memberService = new Mock<IMemberService>();
        //    memberService.Setup(s => s.GetByIds(It.IsAny<string[]>(), It.IsAny<string[]>())).Returns<string[], string[]>((ids, types) => {
        //        return new[] { new Contact() };
        //    });

        //    var logService = new Mock<ILog>();

        //    var avaTaxRateProvider = new AvaTaxRateProvider(memberService.Object, logService.Object, _settings.ToArray());
        //    var validCart = GetInvalidTestCart(Guid.NewGuid().ToString());

        //    //act
        //    avaTaxRateProvider.CalculateCartTax(validCart);

        //    //assert
        //    Assert.All(validCart.Items, item => Assert.True(item.TaxTotal > 0));
        //}

        private AvaTaxClient CreateAvaTaxClient()
        {
            var machineName = Environment.MachineName;
            var avaTaxUri = new Uri(avalaraServiceUrl);
            var result = new AvaTaxClient(ModuleConstants.Avalara.ApplicationName, ModuleConstants.Avalara.ApplicationVersion, machineName, avaTaxUri)
                .WithSecurity(avalaraUsername, avalaraPassword);

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

        private static ICollection<coreTax.TaxLine> GetContextTaxLines()
        {
            var item1 = new coreTax.TaxLine
            {
                Id = Guid.NewGuid().ToString(),
                Price = 20,
                Name = "shoes",
                Code = "SKU1",
                Amount = 1
            };
            var item2 = new coreTax.TaxLine
            {
                Id = Guid.NewGuid().ToString(),
                Price = 100,
                Name = "t-shirt",
                Code = "SKU2",
                Amount = 1
            };

            return new[] { item1, item2 };
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
                    new Address {
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
             
            };
            var item1 = new LineItem
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
              
            };
            var item2 = new LineItem
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
            };
            order.Items = new List<LineItem>();
            order.Items.Add(item1);
            order.Items.Add(item2);

            var shipment = new Shipment
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
            };
            order.Shipments = new List<Shipment>();
            order.Shipments.Add(shipment);

            var payment = new PaymentIn
            {
                Id = Guid.NewGuid().ToString(),
                GatewayCode = "PayPal",
                Currency = "USD",
                Sum = 10,
                CustomerId = "et",
                IsApproved = false
            };
            order.InPayments = new List<PaymentIn> { payment };

            return order;
        }

        private static ShoppingCart GetTestCart(string id)
        {
            var cart = new ShoppingCart
            {
                Id = id,
                Currency = "USD",
                CustomerId = "Test Customer",
                StoreId = "test store",
                CreatedDate = DateTime.Now,
                Addresses = new[]
                {
                    new CartAddress {
                    AddressType = CartAddressType.Shipping,
                    Phone = "+68787687",
                    PostalCode = "60602",
                    CountryCode = "US",
                    CountryName = "United states",
                    Email = "user@mail.com",
                    FirstName = "first name",
                    LastName = "last name",
                    Line1 = "45 Fremont Street",
                    City = "Los Angeles",
                    RegionId = "CA",
                    Organization = "org1"
                    }
                }.ToList(),
                Discounts = new[] { new Discount
                    {
                        PromotionId = "testPromotion",
                        Currency = "USD",
                        DiscountAmount = 12,

                    }
                },

            };
            var item1 = new CartLineItem
            {
                Id = Guid.NewGuid().ToString(),
                ListPrice = 20,
                DiscountAmount = 10,
                ProductId = "shoes",
                CatalogId = "catalog",
                Currency = "USD",
                CategoryId = "category",
                Name = "shoes",
                Quantity = 2,
                ShipmentMethodCode = "EMS",
                Discounts = new[] {
                    new Discount
                    {
                        PromotionId = "itemPromotion",
                        Currency = "USD",
                        DiscountAmount = 12

                    }
                }
            };
            var item2 = new CartLineItem
            {
                Id = Guid.NewGuid().ToString(),
                ListPrice = 100,
                SalePrice = 200,
                ProductId = "t-shirt",
                CatalogId = "catalog",
                CategoryId = "category",
                Currency = "USD",
                Name = "t-shirt",
                Quantity = 2,
                ShipmentMethodCode = "EMS",
                Discounts = new[]{
                    new Discount
                    {
                        PromotionId = "testPromotion",
                        Currency = "USD",
                        DiscountAmount = 12
                    }
                }
            };
            cart.Items = new List<CartLineItem> { item1, item2 };

            var shipment = new CartShipment
            {
                Id = Guid.NewGuid().ToString(),
                Currency = "USD",
                DeliveryAddress = new CartAddress
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
                Discounts = new[] {
                    new Discount
                    {
                        PromotionId = "testPromotion",
                        Currency = "USD",
                        DiscountAmount = 12,

                    }
                }
            };
            cart.Shipments = new List<CartShipment> { shipment };

            var payment = new CartPayment
            {
                Id = Guid.NewGuid().ToString(),
                PaymentGatewayCode = "PayPal",
                Currency = "USD",
                Amount = 10,
                OuterId = "et"
            };
            cart.Payments = new List<CartPayment> { payment };

            return cart;
        }

        private static ShoppingCart GetInvalidTestCart(string id)
        {
            var cart = new ShoppingCart
            {
                Id = id,
                Currency = "USD",
                CustomerId = "Test Customer",
                StoreId = "test store",
                CreatedDate = DateTime.Now,
                Addresses = new[]
                {
                    new CartAddress {
                    AddressType = CartAddressType.Shipping,
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
                Discounts = new[] { new Discount
                    {
                        PromotionId = "testPromotion",
                        Currency = "USD",
                        DiscountAmount = 12,

                    }
                },

            };
            var item1 = new CartLineItem
            {
                Id = Guid.NewGuid().ToString(),
                ListPrice = 10,
                SalePrice = 20,
                ProductId = "shoes",
                CatalogId = "catalog",
                Currency = "USD",
                CategoryId = "category",
                Name = "shoes",
                Quantity = 2,
                ShipmentMethodCode = "EMS",
                Discounts = new[] {
                    new Discount
                    {
                        PromotionId = "itemPromotion",
                        Currency = "USD",
                        DiscountAmount = 12

                    }
                }
            };
            var item2 = new CartLineItem
            {
                Id = Guid.NewGuid().ToString(),
                ListPrice = 100,
                SalePrice = 200,
                ProductId = "t-shirt",
                CatalogId = "catalog",
                CategoryId = "category",
                Currency = "USD",
                Name = "t-shirt",
                Quantity = 2,
                ShipmentMethodCode = "EMS",
                Discounts = new[]{
                    new Discount
                    {
                        PromotionId = "testPromotion",
                        Currency = "USD",
                        DiscountAmount = 12
                    }
                }
            };
            cart.Items = new List<CartLineItem> { item1, item2 };

            var shipment = new CartShipment
            {
                Id = Guid.NewGuid().ToString(),
                Currency = "USD",
                DeliveryAddress = new CartAddress
                {
                    City = "Philadelphia",
                    CountryName = "United states",
                    Phone = "+68787687",
                    PostalCode = "19142",
                    CountryCode = "US",
                    Email = "user@mail.com",
                    FirstName = "first name",
                    LastName = "last name",
                    Line1 = "6025 Greenway Ave",
                    RegionId = "PA",
                    RegionName = "Pennsylvania",
                    Organization = "org1"
                },
                Discounts = new[] {
                    new Discount
                    {
                        PromotionId = "testPromotion",
                        Currency = "USD",
                        DiscountAmount = 12,

                    }
                }
            };
            cart.Shipments = new List<CartShipment> { shipment };

            var payment = new CartPayment
            {
                Id = Guid.NewGuid().ToString(),
                PaymentGatewayCode = "PayPal",
                Currency = "USD",
                Amount = 10,
                OuterId = "et"
            };
            cart.Payments = new List<CartPayment> { payment };

            return cart;
        }

        private AvaTaxController GetTaxController()
        {
            const string _isValidateAddressPropertyName = "Avalara.Tax.IsValidateAddress";

            var settingsManager = new Mock<ISettingsManager>();

            settingsManager.Setup(manager => manager.GetValue(_usernamePropertyName, string.Empty)).Returns(() => _settings.First(x => x.Name == _usernamePropertyName).Value);
            settingsManager.Setup(manager => manager.GetValue(_passwordPropertyName, string.Empty)).Returns(() => _settings.First(x => x.Name == _passwordPropertyName).Value);
            settingsManager.Setup(manager => manager.GetValue(_serviceUrlPropertyName, string.Empty)).Returns(() => _settings.First(x => x.Name == _serviceUrlPropertyName).Value);
            settingsManager.Setup(manager => manager.GetValue(_companyCodePropertyName, string.Empty)).Returns(() => _settings.First(x => x.Name == _companyCodePropertyName).Value);
            settingsManager.Setup(manager => manager.GetValue(_isEnabledPropertyName, true)).Returns(() => true);
            settingsManager.Setup(manager => manager.GetValue(_isValidateAddressPropertyName, true)).Returns(() => true);

            var avalaraTax = new AvaTaxSettings(_usernamePropertyName, _passwordPropertyName, _serviceUrlPropertyName, _companyCodePropertyName, _isEnabledPropertyName, _isValidateAddressPropertyName, settingsManager.Object);
            var logger = new Mock<ILog>();

            var controller = new AvaTaxController(avalaraTax, logger.Object, CreateAvaTaxClient);
            return controller;
        }
    }
}
