using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Services.Payments;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Ecommpay.Components
{
    /// <summary>
    /// Represents a view component to add the libraries links in public store
    /// </summary>
    [ViewComponent(Name = Defaults.HEAD_LINKS_VIEW_COMPONENT_NAME)]
    public class HeadLinksViewComponent : NopViewComponent
    {
        #region Fields

        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;

        #endregion

        #region Ctor

        public HeadLinksViewComponent(
            IPaymentPluginManager paymentPluginManager,
            IWorkContext workContext,
            IStoreContext storeContext
        )
        {
            _paymentPluginManager = paymentPluginManager;
            _workContext = workContext;
            _storeContext = storeContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invokes a view component
        /// </summary>
        /// <returns>The <see cref="Task"/> containing the <see cref="IViewComponentResult"/></returns>
        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!await _paymentPluginManager.IsPluginActiveAsync(Defaults.SystemName, await _workContext.GetCurrentCustomerAsync(), (await _storeContext.GetCurrentStoreAsync()).Id))
                return Content(string.Empty);

            var routeName = HttpContext.GetEndpoint()?.Metadata.GetMetadata<RouteNameMetadata>()?.RouteName;
            return routeName == Defaults.CheckoutOnePageRouteName || routeName == Defaults.CheckoutPaymentInfoRouteName
                ? View("~/Plugins/Payments.ECommPay/Views/HeadLinks.cshtml")
                : Content(string.Empty);
        }

        #endregion
    }
}
