using System;
using System.Net.Http;
using System.Threading.Tasks;
using Nop.Plugin.Payments.Ecommpay;
using Nop.Plugin.Payments.Ecommpay.Services;
using Nop.Plugin.Payments.ECommPay.Models;

namespace Nop.Plugin.Payments.ECommPay.Services
{
    /// <summary>
    /// Provides an default implementation of the HTTP client to interact with the ECommPay endpoints
    /// </summary>
    public class EcommpayApi : BaseHttpClient
    {
        #region Ctor

        public EcommpayApi(HttpClient httpClient)
            : base(httpClient)
        {
        }

        #endregion

        #region Methods

        public virtual Task<CreateRefundResponse> CreateRefundAsync(CreateRefundRequest request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            return PostAsync<CreateRefundResponse>(Defaults.ECommPay.API.Endpoints.Refund, request);
        }

        #endregion
    }
}
