using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.Ecommpay.Domain;
using Nop.Plugin.Payments.Ecommpay.Services;
using Nop.Services.Logging;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Ecommpay.Components
{
    /// <summary>
    /// Represents a view component to display payment info in public store
    /// </summary>
    [ViewComponent(Name = Defaults.PAYMENT_INFO_VIEW_COMPONENT_NAME)]
    public class PaymentInfoViewComponent : NopViewComponent
    {
        #region Fields

        private readonly EcommpayService _ecommpayService;
        private readonly EcommpayPaymentSettings _ecommpayPaymentSettings;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public PaymentInfoViewComponent(
            EcommpayService ecommpayService,
            EcommpayPaymentSettings ecommpayPaymentSettings,
            ILogger logger
        )
        {
            _ecommpayService = ecommpayService;
            _ecommpayPaymentSettings = ecommpayPaymentSettings;
            _logger = logger;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invokes a view component
        /// </summary>
        /// <returns>The <see cref="Task"/> containing the <see cref="IViewComponentResult"/></returns>
        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (_ecommpayPaymentSettings.PaymentFlowType != PaymentFlowType.Iframe)
                return Content(string.Empty);

            var model = await _ecommpayService.CreateIframePaymentPageModelAsync();
            if (!model.Success)
                await _logger.ErrorAsync($"{Defaults.SystemName}: Error when opening the Payment Page embedded in a checkout.{Environment.NewLine}{string.Join(Environment.NewLine, model.Errors)}");

            return View("~/Plugins/Payments.ECommPay/Views/PaymentInfo.cshtml", model);
        }

        #endregion
    }
}
