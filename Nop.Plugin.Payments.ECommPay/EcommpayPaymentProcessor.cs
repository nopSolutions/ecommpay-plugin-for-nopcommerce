using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Ecommpay.Domain;
using Nop.Plugin.Payments.Ecommpay.Extensions;
using Nop.Plugin.Payments.Ecommpay.Services;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Framework.UI;

namespace Nop.Plugin.Payments.Ecommpay
{
    public class EcommpayPaymentProcessor : BasePlugin, IPaymentMethod, IWidgetPlugin
    {
        #region Fields

        private readonly EcommpayPaymentSettings _ecommpayPaymentSettings;
        private readonly EcommpayService _ecommpayService;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IPaymentService _paymentService;
        private readonly IPageHeadBuilder _pageHeadBuilder;
        private readonly INotificationService _notificationService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly WidgetSettings _widgetSettings;

        #endregion

        #region Ctor

        public EcommpayPaymentProcessor(
            EcommpayPaymentSettings ecommpayPaymentSettings,
            EcommpayService ecommpayService,
            IActionContextAccessor actionContextAccessor,
            ILocalizationService localizationService,
            ILogger logger,
            IPaymentService paymentService,
            IPageHeadBuilder pageHeadBuilder,
            INotificationService notificationService,
            IUrlHelperFactory urlHelperFactory,
            ISettingService settingService,
            IWebHelper webHelper,
            WidgetSettings widgetSettings
        )
        {
            _ecommpayPaymentSettings = ecommpayPaymentSettings;
            _ecommpayService = ecommpayService;
            _actionContextAccessor = actionContextAccessor;
            _localizationService = localizationService;
            _logger = logger;
            _paymentService = paymentService;
            _pageHeadBuilder = pageHeadBuilder;
            _notificationService = notificationService;
            _urlHelperFactory = urlHelperFactory;
            _settingService = settingService;
            _webHelper = webHelper;
            _widgetSettings = widgetSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>The <see cref="Task"/> containing the <see cref="ProcessPaymentResult"/></returns>
        public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult());
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            if (postProcessPaymentRequest is null)
                throw new ArgumentNullException(nameof(postProcessPaymentRequest));

            if (_ecommpayPaymentSettings.PaymentFlowType != PaymentFlowType.NewBrowserTab)
                return;

            var order = postProcessPaymentRequest.Order;
            var model = await _ecommpayService.CreateNewBrowserTabPaymentPageModelAsync(order);
            if (model.Success)
                _actionContextAccessor.ActionContext.HttpContext.Response.Redirect(model.Url);
            else
            {
                await _logger.ErrorAsync($"{Defaults.SystemName}: Error when opening the Payment Page in the new browser tab.{Environment.NewLine}{string.Join(Environment.NewLine, model.Errors)}");

                var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);
                var failUrl = urlHelper.RouteUrl(Defaults.OrderDetailsRouteName, new { orderId = order.Id }, _webHelper.GetCurrentRequestProtocol());

                _notificationService.ErrorNotification(
                    await _localizationService.GetResourceAsync("Plugins.Payments.Ecommpay.FailedOrderCreation"));

                _actionContextAccessor.ActionContext.HttpContext.Response.Redirect(failUrl);
            }
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>The <see cref="Task"/> containing a value indicating whether payment method should be hidden during checkout</returns>
        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>The <see cref="Task"/> containing a additional handling fee</returns>
        public async Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return await _paymentService.CalculateAdditionalFeeAsync(cart,
                _ecommpayPaymentSettings.AdditionalFee, _ecommpayPaymentSettings.AdditionalFeePercentage);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>The <see cref="Task"/> containing the <see cref="CapturePaymentResult"/></returns>
        public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            return Task.FromResult(new CapturePaymentResult { Errors = new[] { "Capture method not supported" } });
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>The <see cref="Task"/> containing the <see cref="RefundPaymentResult"/></returns>
        public async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            if (refundPaymentRequest is null)
                throw new ArgumentNullException(nameof(refundPaymentRequest));

            var order = refundPaymentRequest.Order;
            var amountToRefund = refundPaymentRequest.IsPartialRefund
                ? (decimal?)refundPaymentRequest.AmountToRefund
                : null;

            // send refund request and wait for ECommPay response via the webhook request
            var result = await _ecommpayService.RefundOrderAsync(order, amountToRefund);
            if (result.Success)
            {
                //change error notification to warning
                _pageHeadBuilder.AddCssFileParts(ResourceLocation.Head, @"~/Plugins/Payments.ECommPay/Areas/Admin/Content/styles.css", string.Empty);

                return new RefundPaymentResult
                {
                    Errors = new List<string> { await _localizationService.GetResourceAsync("Plugins.Payments.Ecommpay.RefundIsCreated") }
                };
            }

            return new RefundPaymentResult { Errors = result.Errors };
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>The <see cref="Task"/> containing the <see cref="VoidPaymentResult"/></returns>
        public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            return Task.FromResult(new VoidPaymentResult { Errors = new[] { "Void method not supported" } });
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>The <see cref="Task"/> containing the <see cref="ProcessPaymentResult"/></returns>
        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } });
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>The <see cref="Task"/> containing the <see cref="CancelRecurringPaymentResult"/></returns>
        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return Task.FromResult(new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } });
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>The <see cref="Task"/> containing a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)</returns>
        public async Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (!(await _ecommpayService.ValidateAsync()).IsValid)
                return false;

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return false;

            return true;
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>The <see cref="Task"/> containing the list of validating errors</returns>
        public async Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            return (await _ecommpayService.ValidateAsync()).Errors;
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>The <see cref="Task"/> containing the payment info holder</returns>
        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            return Task.FromResult(new ProcessPaymentRequest());
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return _urlHelperFactory
                .GetUrlHelper(_actionContextAccessor.ActionContext)
                .RouteUrl(Defaults.ConfigurationRouteName);
        }

        /// <summary>
        /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
        public string GetPublicViewComponentName()
        {
            return Defaults.PAYMENT_INFO_VIEW_COMPONENT_NAME;
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>The <see cref="Task"/></returns>
        public async override Task InstallAsync()
        {
            //settings
            await _settingService.SaveSettingAsync(new EcommpayPaymentSettings
            {
                IsTestMode = true,
                PaymentFlowType = PaymentFlowType.NewBrowserTab,
            });

            if (!_widgetSettings.ActiveWidgetSystemNames.Contains(Defaults.SystemName))
            {
                _widgetSettings.ActiveWidgetSystemNames.Add(Defaults.SystemName);
                await _settingService.SaveSettingAsync(_widgetSettings);
            }

            //locales
            await _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Enums.Nop.Plugin.Payments.Ecommpay.Domain.PaymentFlowType.NewBrowserTab"] = "Open Payment Page in a separate browser tab",
                ["Enums.Nop.Plugin.Payments.Ecommpay.Domain.PaymentFlowType.Iframe"] = "Open Payment Page embedded in a checkout",
                ["Plugins.Payments.Ecommpay.AdditionalParameters.Customer.PersonalData"] = "Customer personal data",
                ["Plugins.Payments.Ecommpay.AdditionalParameters.Customer.BillingAddress"] = "Customer billing data",
                ["Plugins.Payments.Ecommpay.Fields.CallbackEndpoint"] = "Callback endpoint",
                ["Plugins.Payments.Ecommpay.Fields.CallbackEndpoint.Hint"] = "The URL to receive callbacks about transactions. Provide it to the ECommPay specialists during integration.",
                ["Plugins.Payments.Ecommpay.Fields.IsTestMode"] = "Test mode",
                ["Plugins.Payments.Ecommpay.Fields.IsTestMode.Hint"] = "Determine whether to use the test environment.",
                ["Plugins.Payments.Ecommpay.Fields.TestProjectId"] = "Test project ID",
                ["Plugins.Payments.Ecommpay.Fields.TestProjectId.Hint"] = "Enter the project ID provided by ECOMMPAY for testing purposes.",
                ["Plugins.Payments.Ecommpay.Fields.TestProjectId.Required"] = "The test project ID is required.",
                ["Plugins.Payments.Ecommpay.Fields.TestProjectId.ShouldBeNumeric"] = "The test project ID must be numeric.",
                ["Plugins.Payments.Ecommpay.Fields.ProductionProjectId"] = "Production project ID",
                ["Plugins.Payments.Ecommpay.Fields.ProductionProjectId.Hint"] = "Enter the project ID provided by ECOMMPAY for production environment.",
                ["Plugins.Payments.Ecommpay.Fields.ProductionProjectId.Required"] = "The production project ID is required.",
                ["Plugins.Payments.Ecommpay.Fields.ProductionProjectId.ShouldBeNumeric"] = "The production project ID must be numeric.",
                ["Plugins.Payments.Ecommpay.Fields.TestSecretKey"] = "Test secret key",
                ["Plugins.Payments.Ecommpay.Fields.TestSecretKey.Hint"] = "Enter the secret key provided by ECOMMPAY for testing purposes.",
                ["Plugins.Payments.Ecommpay.Fields.TestSecretKey.Required"] = "The test secret key is required.",
                ["Plugins.Payments.Ecommpay.Fields.ProductionSecretKey"] = "Production secret key",
                ["Plugins.Payments.Ecommpay.Fields.ProductionSecretKey.Hint"] = "Enter the secret key provided by ECOMMPAY for production environment.",
                ["Plugins.Payments.Ecommpay.Fields.ProductionSecretKey.Required"] = "The production secret key is required.",
                ["Plugins.Payments.Ecommpay.Fields.PaymentFlowTypeId"] = "Payment flow",
                ["Plugins.Payments.Ecommpay.Fields.PaymentFlowTypeId.Hint"] = "Select the payment flow type for users in checkout.",
                ["Plugins.Payments.Ecommpay.Fields.AdditionalParameterSystemNames"] = "Additional parameters",
                ["Plugins.Payments.Ecommpay.Fields.AdditionalParameterSystemNames.Hint"] = "Select the additional parameters to pass to the ECOMMPAY.",
                ["Plugins.Payments.Ecommpay.Fields.AdditionalFee"] = "Additional fee",
                ["Plugins.Payments.Ecommpay.Fields.AdditionalFee.Hint"] = "Enter additional fee to charge your customers.",
                ["Plugins.Payments.Ecommpay.Fields.AdditionalFeePercentage"] = "Additional fee. Use percentage",
                ["Plugins.Payments.Ecommpay.Fields.AdditionalFeePercentage.Hint"] = "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.",
                ["Plugins.Payments.Ecommpay.FailedOrderCreation"] = "Error when processing the payment transaction. Please try again or contact with store owner.",
                ["Plugins.Payments.Ecommpay.PaymentMethodDescription"] = "Pay by ECommPay",
                ["Plugins.Payments.Ecommpay.RefundIsCreated"] = "Refund request is sended. The refund performing period depends on the issuing bank and may take a long time. The request result will be displayed in the order notes.",
            });

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        /// <returns>The <see cref="Task"/></returns>
        public async override Task UninstallAsync()
        {
            //settings
            await _settingService.DeleteSettingAsync<EcommpayPaymentSettings>();

            //settings
            if (_widgetSettings.ActiveWidgetSystemNames.Contains(Defaults.SystemName))
            {
                _widgetSettings.ActiveWidgetSystemNames.Remove(Defaults.SystemName);
                await _settingService.SaveSettingAsync(_widgetSettings);
            }

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.Ecommpay");
            await _localizationService.DeleteLocaleResourcesAsync("Enums.Nop.Plugin.Payments.Ecommpay");

            await base.UninstallAsync();
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        /// <returns>The <see cref="Task"/> containing the payment method description that will be displayed on checkout pages in the public store</returns>
        public Task<string> GetPaymentMethodDescriptionAsync()
        {
            return _localizationService.GetResourceAsync("Plugins.Payments.Ecommpay.PaymentMethodDescription");
        }

        /// <summary>
        /// Gets widget zones where this widget should be rendered
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the widget zones
        /// </returns>
        public Task<IList<string>> GetWidgetZonesAsync()
        {
            return Task.FromResult<IList<string>>(new List<string>
            {
                PublicWidgetZones.HeadHtmlTag
            });
        }

        /// <summary>
        /// Gets a name of a view component for displaying widget
        /// </summary>
        /// <param name="widgetZone">Name of the widget zone</param>
        /// <returns>View component name</returns>
        public string GetWidgetViewComponentName(string widgetZone)
        {
            if (widgetZone == null)
                throw new ArgumentNullException(nameof(widgetZone));

            if (widgetZone.Equals(PublicWidgetZones.HeadHtmlTag))
                return Defaults.HEAD_LINKS_VIEW_COMPONENT_NAME;

            return string.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
        /// </summary>
        public bool HideInWidgetList => true;

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => false;

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => true;

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => true;

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid => false;

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => _ecommpayPaymentSettings.PaymentFlowType.ToPaymentMethodType();

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo => _ecommpayPaymentSettings.PaymentFlowType == PaymentFlowType.NewBrowserTab;

        #endregion
    }
}
