using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Http.Extensions;
using Nop.Core.Infrastructure;
using Nop.Plugin.Payments.Ecommpay.Areas.Admin.Models;
using Nop.Plugin.Payments.Ecommpay.Domain;
using Nop.Plugin.Payments.Ecommpay.Models;
using Nop.Plugin.Payments.ECommPay.Helpers;
using Nop.Plugin.Payments.ECommPay.Models;
using Nop.Plugin.Payments.ECommPay.Services;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.Ecommpay.Services
{
    public class EcommpayService
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly CustomerSettings _customerSettings;
        private readonly EcommpayApi _ecommpayApi;
        private readonly EcommpayPaymentSettings _ecommpayPaymentSettings;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IAddressService _addressService;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly IPaymentService _paymentService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IOrderService _orderService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IWorkContext _workContext;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public EcommpayService(
            CurrencySettings currencySettings,
            CustomerSettings customerSettings,
            EcommpayApi ecommpayApi,
            EcommpayPaymentSettings ecommpayPaymentSettings,
            IActionContextAccessor actionContextAccessor,
            IAddressService addressService,
            ICountryService countryService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IPaymentService paymentService,
            IGenericAttributeService genericAttributeService,
            IOrderService orderService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IOrderProcessingService orderProcessingService,
            IHttpContextAccessor httpContextAccessor,
            IStateProvinceService stateProvinceService,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            IUrlHelperFactory urlHelperFactory,
            IWorkContext workContext,
            IWebHelper webHelper
        )
        {
            _currencySettings = currencySettings;
            _customerSettings = customerSettings;
            _ecommpayApi = ecommpayApi;
            _currencyService = currencyService;
            _customerService = customerService;
            _paymentService = paymentService;
            _genericAttributeService = genericAttributeService;
            _orderService = orderService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _orderProcessingService = orderProcessingService;
            _httpContextAccessor = httpContextAccessor;
            _stateProvinceService = stateProvinceService;
            _shoppingCartService = shoppingCartService;
            _storeContext = storeContext;
            _urlHelperFactory = urlHelperFactory;
            _workContext = workContext;
            _webHelper = webHelper;
            _ecommpayPaymentSettings = ecommpayPaymentSettings;
            _actionContextAccessor = actionContextAccessor;
            _addressService = addressService;
            _countryService = countryService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the errors as <see cref="IList{string}"/> if plugin isn't configured; otherwise empty
        /// </summary>
        /// <returns>The <see cref="Task"/> containing the errors as <see cref="IList{string}"/> if plugin isn't configured; otherwise empty</returns>
        public virtual async Task<(bool IsValid, IList<string> Errors)> ValidateAsync()
        {
            // resolve validator here to exclude warnings after installation process
            var validator = EngineContext.Current.Resolve<IValidator<ConfigurationModel>>();
            var validationResult = await validator.ValidateAsync(new ConfigurationModel
            {
                IsTestMode = _ecommpayPaymentSettings.IsTestMode,
                TestProjectId = _ecommpayPaymentSettings.TestProjectId.ToString(),
                TestSecretKey = _ecommpayPaymentSettings.TestSecretKey,
                ProductionProjectId = _ecommpayPaymentSettings.ProductionProjectId.ToString(),
                ProductionSecretKey = _ecommpayPaymentSettings.ProductionSecretKey,
            });

            return (validationResult.IsValid, validationResult.Errors.Select(error => error.ErrorMessage).ToList());
        }

        /// <summary>
        /// Creates the ECOMMPAY Payment Page model as <see cref="PaymentFlowType.Iframe"/>
        /// </summary>
        /// <returns>The <see cref="Task"/> containing the <see cref="CreatePaymentPageModel"/></returns>
        public virtual async Task<CreatePaymentPageModel> CreateIframePaymentPageModelAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            httpContext.Session.Remove(Defaults.PaymentRequestSessionKey);

            var processPaymentRequest = new ProcessPaymentRequest();
            _paymentService.GenerateOrderGuid(processPaymentRequest);

            httpContext.Session.Set(Defaults.PaymentRequestSessionKey, processPaymentRequest);

            var model = new CreatePaymentPageModel();

            await PrepareCommonQueryAsync(model);

            var customer = await _workContext.GetCurrentCustomerAsync();
            if (customer == null)
                model.Errors.Add($"Cannot get the current customer.");

            if (model.Errors.Count > 0)
                return model;

            var paymentAmount = 0;
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
            if (cart == null || cart.Count == 0)
                model.Errors.Add($"Cart is empty.");
            else
            {
                var subTotal = (await _orderTotalCalculationService.GetShoppingCartTotalAsync(cart)).shoppingCartTotal;
                if (!subTotal.HasValue || subTotal.Value == decimal.Zero)
                    model.Errors.Add($"Cart total should be greater then 0.");
                else
                    paymentAmount = (int)(subTotal.Value * 100);
            }

            if (model.Errors.Count > 0)
                return model;

            model.BaseUrl = Defaults.ECommPay.PaymentPage.Host;

            model.Query.Add(new("customer_id", customer.Id.ToString()));
            model.Query.Add(new("customer_account_number", customer.Email));
            model.Query.Add(new("payment_id", processPaymentRequest.OrderGuid.ToString()));
            model.Query.Add(new("payment_amount", paymentAmount.ToString(CultureInfo.InvariantCulture)));
            model.Query.Add(new("target_element", Defaults.ECommPay.PaymentPage.ContainerName));

            var billingAddress = await _customerService.GetCustomerBillingAddressAsync(customer);
            await PrepareAdditionalParametersAsync(model, billingAddress, customer);

            PrepareSignature(model);

            return model;
        }

        /// <summary>
        /// Creates the ECOMMPAY Payment Page model as <see cref="PaymentFlowType.NewBrowserTab"/>
        /// </summary>
        /// <param name="order">The order</param>
        /// <returns>The <see cref="Task"/> containing the <see cref="CreatePaymentPageModel"/></returns>
        public virtual async Task<CreatePaymentPageModel> CreateNewBrowserTabPaymentPageModelAsync(Order order)
        {
            if (order is null)
                throw new ArgumentNullException(nameof(order));

            var model = new CreatePaymentPageModel();

            await PrepareCommonQueryAsync(model);

            var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
            if (customer == null)
                model.Errors.Add($"The customer with id '{order.CustomerId}' not found.");

            if (model.Errors.Count > 0)
                return model;

            var currentRequestProtocol = _webHelper.GetCurrentRequestProtocol();
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);
            var callbackUrl = urlHelper.RouteUrl(Defaults.OrderDetailsRouteName, new { orderId = order.Id }, currentRequestProtocol);
            var successfulUrl = urlHelper.RouteUrl(Defaults.CheckoutCompletedRouteName, new { orderId = order.Id }, currentRequestProtocol);

            model.BaseUrl = Defaults.ECommPay.PaymentPage.Url;

            model.Query.Add(new("customer_id", customer.Id.ToString()));
            model.Query.Add(new("customer_account_number", customer.Email));
            model.Query.Add(new("payment_id", order.OrderGuid.ToString()));
            model.Query.Add(new("payment_amount", ((int)(order.OrderTotal * 100)).ToString(CultureInfo.InvariantCulture)));
            model.Query.Add(new("redirect", "1"));
            model.Query.Add(new("merchant_success_url", successfulUrl));
            model.Query.Add(new("merchant_fail_url", callbackUrl));
            model.Query.Add(new("merchant_return_url", callbackUrl));

            var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
            await PrepareAdditionalParametersAsync(model, billingAddress, customer);

            PrepareSignature(model);

            return model;
        }

        /// <summary>
        /// Creates the refund for order with amount (partial refund or pass null for full refund)
        /// </summary>
        /// <param name="order">The order</param>
        /// <param name="amountToRefund">The amount for partial refund or pass null for full refund.</param>
        /// <returns>The <see cref="Task"/> containing the result of processing the refund creating.</returns>
        public virtual async Task<(bool Success, IList<string> Errors)> RefundOrderAsync(Order order, decimal? amountToRefund)
        {
            if (order is null)
                throw new ArgumentNullException(nameof(order));

            var errors = new List<string>();

            var validationResult = await ValidateAsync();
            if (!validationResult.IsValid)
                errors.AddRange(validationResult.Errors);

            var storeCurrency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
            if (storeCurrency == null)
                errors.Add($"The primary store currency with ID: '{_currencySettings.PrimaryStoreCurrencyId}' not found.");

            if (errors.Count > 0)
                return (false, errors);

            var request = new CreateRefundRequest
            {
                General = new RefundGeneralPayload
                {
                    ProjectId = _ecommpayPaymentSettings.IsTestMode
                        ? _ecommpayPaymentSettings.TestProjectId
                        : _ecommpayPaymentSettings.ProductionProjectId,
                    PaymentId = order.CaptureTransactionId,
                },
                Payment = new RefundPaymentPayload
                {
                    Description = $"Refund for the order '{order.CustomOrderNumber}'.",
                }
            };

            if (amountToRefund.HasValue)
            {
                request.Payment.Amount = (int)(amountToRefund * 100);
                request.Payment.Currency = storeCurrency.CurrencyCode;
            }

            var signature = GenerateSignature(request);
            if (signature == null)
            {
                errors.Add($"Error when generating the signature. Check to the secret key is valid in plugin settings.");
                return (false, errors);
            }

            request.General.Signature = signature;

            try
            {
                var createRefundResponse = await _ecommpayApi.CreateRefundAsync(request);
                if (createRefundResponse == null)
                {
                    errors.Add($"Cannot create the refund for the order '{order.CustomOrderNumber}'. The ECommpay endpoint isn't responding.");
                    return (false, errors);
                }

                return (true, errors);
            }
            catch (ApiException ex)
            {
                errors.Add(ex.Message);
            }

            return (false, errors);
        }

        /// <summary>
        /// Processes the web hook request
        /// </summary>
        /// <param name="httpRequest">The HTTP request.</param>
        /// <returns>The <see cref="Task"/> containing the result of processing the web hook request.</returns>
        public virtual async Task<(bool Success, IList<string> Errors)> ProcessWebHookRequestAsync(HttpRequest httpRequest)
        {
            if (httpRequest == null)
                throw new ArgumentNullException(nameof(httpRequest));

            var errors = new List<string>();

            var validationResult = await ValidateAsync();
            if (!validationResult.IsValid)
            {
                errors.AddRange(validationResult.Errors);
                return (false, errors);
            }

            using var streamReader = new StreamReader(httpRequest.Body);
            var rawBody = await streamReader.ReadToEndAsync();

            if (!ValidateSignature(rawBody))
            {
                errors.Add("Invalid signature verification. Make sure that the correct 'Secret key' is specified in the plugin settings.");
                return (false, errors);
            }

            WebHookRequest request = null;
            try
            {
                request = JsonConvert.DeserializeObject<WebHookRequest>(rawBody);
            }
            catch (Exception)
            {
                errors.Add($"Invalid the deserialization of the '{nameof(HttpRequest)}.{nameof(HttpRequest.Body)}' to the '{nameof(WebHookRequest)}'.");
                return (false, errors);
            }

            return request?.Operation?.Type switch
            {
                "sale" => await ProcessSaleOperationAsync(request),
                "refund" => await ProcessRefundOperationAsync(request),
                _ => (true, errors)
            };
        }

        #endregion

        #region Utilities

        private async Task<(bool Success, IList<string> Errors)> ProcessSaleOperationAsync(WebHookRequest request)
        {
            var errors = new List<string>();

            // only successful payments
            if (request.Operation.Status != "success")
                return (true, errors);

            if (!Guid.TryParse(request.Payment?.Id, out var orderGuid))
                return (true, errors);

            var order = await _orderService.GetOrderByGuidAsync(orderGuid);
            if (order == null)
            {
                errors.Add($"The order not found by the specified payment ID '{orderGuid}'.");
                return (false, errors);
            }

            if (order.Deleted)
                return (true, errors);

            if (!_orderProcessingService.CanMarkOrderAsPaid(order))
                return (true, errors);

            order.CaptureTransactionId = orderGuid.ToString();
            await _orderProcessingService.MarkOrderAsPaidAsync(order);

            return (true, errors);
        }

        private async Task<(bool Success, IList<string> Errors)> ProcessRefundOperationAsync(WebHookRequest request)
        {
            var errors = new List<string>();

            if (!Guid.TryParse(request.Payment?.Id, out var orderGuid))
                return (true, errors);

            var order = await _orderService.GetOrderByGuidAsync(orderGuid);
            if (order == null)
            {
                errors.Add($"The order not found by the specified payment ID '{orderGuid}'.");
                return (false, errors);
            }

            if (order.Deleted)
                return (true, errors);

            if (request.Operation.InitialSum == null)
                return (true, errors);

            var amountToRefund = (decimal)request.Operation.InitialSum.Amount / 100;

            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                OrderId = order.Id,
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                Note = $"The refund reqest is processed with status '{request.Operation.Status}'.{Environment.NewLine}" +
                    $"The code '{request.Operation.Code}'.{Environment.NewLine}" +
                    $"The message '{request.Operation.Message}'.{Environment.NewLine}" +
                    $"The amount '{amountToRefund} {request.Operation.InitialSum.Currency}'."
            });

            if (request.Operation.Status != "success")
                return (true, errors);

            if (order.OrderTotal == amountToRefund)
            {
                if (_orderProcessingService.CanRefundOffline(order))
                    await _orderProcessingService.RefundOfflineAsync(order);
            }
            else
            {
                if (_orderProcessingService.CanPartiallyRefundOffline(order, amountToRefund))
                    await _orderProcessingService.PartiallyRefundOfflineAsync(order, amountToRefund);
            }

            return (true, errors);
        }

        private async Task PrepareCommonQueryAsync(CreatePaymentPageModel model)
        {
            var validationResult = await ValidateAsync();
            if (!validationResult.IsValid)
            {
                foreach (var errorMessage in validationResult.Errors)
                    model.Errors.Add(errorMessage);
            }

            var storeCurrency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
            if (storeCurrency == null)
                model.Errors.Add($"The primary store currency with ID: '{_currencySettings.PrimaryStoreCurrencyId}' not found.");

            if (model.Errors.Count > 0)
                return;
            
            model.Query.Add(new("payment_currency", storeCurrency.CurrencyCode));
            model.Query.Add(new("project_id", _ecommpayPaymentSettings.IsTestMode 
                ? _ecommpayPaymentSettings.TestProjectId.ToString()
                : _ecommpayPaymentSettings.ProductionProjectId.ToString()));
            model.Query.Add(new("card_operation_type", "sale"));        
        }

        private async Task PrepareAdditionalParametersAsync(CreatePaymentPageModel model, Address billingAddress, Customer customer)
        {
            if (_ecommpayPaymentSettings.AdditionalParameterSystemNames?.Any() == true)
            {
                foreach (var parameterSystemName in _ecommpayPaymentSettings.AdditionalParameterSystemNames)
                {
                    if (parameterSystemName == Defaults.ECommPay.AdditionalParameters.Customer.PersonalData.SystemName)
                    {
                        model.Query.Add(new("customer_email", customer.Email));

                        if (_customerSettings.FirstNameEnabled)
                        {
                            var firstName = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.FirstNameAttribute);
                            if (!string.IsNullOrWhiteSpace(firstName))
                                model.Query.Add(new("customer_first_name", firstName));
                        }

                        if (_customerSettings.LastNameEnabled)
                        {
                            var lastName = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.LastNameAttribute);
                            if (!string.IsNullOrWhiteSpace(lastName))
                                model.Query.Add(new("customer_last_name", lastName));
                        }

                        if (_customerSettings.PhoneEnabled)
                        {
                            var phone = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.PhoneAttribute);
                            if (!string.IsNullOrWhiteSpace(phone))
                                model.Query.Add(new("customer_phone", phone));
                        }

                        if (_customerSettings.DateOfBirthEnabled)
                        {
                            var dateOfBirthRaw = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.DateOfBirthAttribute);
                            if (DateTime.TryParse(dateOfBirthRaw, out var dateOfBirth))
                                model.Query.Add(new("customer_day_of_birth", dateOfBirth.ToString("dd-MM-yyyy")));
                        }

                        if (_customerSettings.CountryEnabled)
                        {
                            var countryId = await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.CountryIdAttribute);
                            var country = await _countryService.GetCountryByIdAsync(countryId);
                            if (country != null)
                                model.Query.Add(new("customer_country", country.TwoLetterIsoCode));
                        }

                        if (_customerSettings.StateProvinceEnabled)
                        {
                            var stateProvinceId = await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.StateProvinceIdAttribute);
                            var stateProvince = await _stateProvinceService.GetStateProvinceByIdAsync(stateProvinceId);
                            if (stateProvince != null)
                                model.Query.Add(new("customer_state", stateProvince.Name));
                        }

                        if (_customerSettings.CityEnabled)
                        {
                            var city = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.CityAttribute);
                            if (!string.IsNullOrWhiteSpace(city))
                                model.Query.Add(new("customer_city", city));
                        }

                        if (_customerSettings.StreetAddressEnabled)
                        {
                            var streetAddress = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.StreetAddressAttribute);
                            if (!string.IsNullOrWhiteSpace(streetAddress))
                                model.Query.Add(new("customer_address", streetAddress));
                        }

                        if (_customerSettings.ZipPostalCodeEnabled)
                        {
                            var zipPostalCode = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.ZipPostalCodeAttribute);
                            if (!string.IsNullOrWhiteSpace(zipPostalCode))
                                model.Query.Add(new("customer_zip", zipPostalCode));
                        }
                    }
                    else if (parameterSystemName == Defaults.ECommPay.AdditionalParameters.Customer.BillingAddress.SystemName)
                    {
                        if (billingAddress != null)
                        {
                            if (billingAddress.CountryId.HasValue)
                            {
                                var billingCountry = await _countryService.GetCountryByIdAsync(billingAddress.CountryId.Value);
                                if (billingCountry != null)
                                    model.Query.Add(new("billing_country", billingCountry.TwoLetterIsoCode));
                            }

                            if (billingAddress.StateProvinceId.HasValue)
                            {
                                var billingStateProvince = await _stateProvinceService.GetStateProvinceByIdAsync(billingAddress.StateProvinceId.Value);
                                if (billingStateProvince != null)
                                    model.Query.Add(new("billing_region_code", billingStateProvince.Abbreviation));
                            }

                            if (!string.IsNullOrWhiteSpace(billingAddress.City))
                                model.Query.Add(new("billing_city", billingAddress.City));

                            if (!string.IsNullOrWhiteSpace(billingAddress.Address1))
                                model.Query.Add(new("billing_address", billingAddress.Address1));

                            if (!string.IsNullOrWhiteSpace(billingAddress.ZipPostalCode))
                                model.Query.Add(new("billing_postal", billingAddress.ZipPostalCode));
                        }
                    }
                }
            }
        }

        private void PrepareSignature(CreatePaymentPageModel model)
        {
            model.Query.Add(new("signature", Signer.GenerateSignature(GetSecretKey(), model.Query)));
        }

        private string GenerateSignature(object model)
        {
            return Signer.GenerateSignature(GetSecretKey(), model);
        }

        private bool ValidateSignature(string payload)
        {
            return Signer.ValidateSignature(GetSecretKey(), payload);
        }

        private string GetSecretKey()
        {
            return _ecommpayPaymentSettings.IsTestMode
                ? _ecommpayPaymentSettings.TestSecretKey
                : _ecommpayPaymentSettings.ProductionSecretKey;
        }

        #endregion
    }
}
