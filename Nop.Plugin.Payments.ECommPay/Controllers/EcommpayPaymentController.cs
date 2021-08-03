using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.Ecommpay.Services;
using Nop.Services.Logging;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.Ecommpay.Controllers
{
    public class EcommpayPaymentController : Controller
    {
        #region Fields

        private readonly EcommpayService _ecommpayService;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public EcommpayPaymentController(
            EcommpayService ecommpayService,
            IPaymentPluginManager paymentPluginManager,
            ILogger logger
        )
        {
            _ecommpayService = ecommpayService;
            _paymentPluginManager = paymentPluginManager;
            _logger = logger;
        }

        #endregion

        #region Methods

        public async Task<IActionResult> Callback()
        {
            // see more about response statuses: https://developers.ecommpay.com/ru/ru_PP_Callbacks.html

            if (await _paymentPluginManager.LoadPluginBySystemNameAsync(Defaults.SystemName) is not EcommpayPaymentProcessor processor || !_paymentPluginManager.IsPluginActive(processor))
                return BadRequest();

            var result = await _ecommpayService.ProcessWebHookRequestAsync(HttpContext.Request);
            if (!result.Success)
            {
                await _logger.ErrorAsync($"{Defaults.SystemName}: Error when processing the web hook request.{Environment.NewLine}{string.Join(Environment.NewLine, result.Errors)}");

                return BadRequest();
            }

            return Ok();
        }

        #endregion
    }
}
