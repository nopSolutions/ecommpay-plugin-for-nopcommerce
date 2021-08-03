using Newtonsoft.Json;

namespace Nop.Plugin.Payments.ECommPay.Models
{
    /// <summary>
    /// Represents a web hook request
    /// </summary>
    public class WebHookRequest
    {
        #region Properties

        /// <summary>
        /// Gets or sets the payment payload.
        /// </summary>
        [JsonProperty("payment")]
        public WebHookPaymentPayload Payment { get; set; }

        /// <summary>
        /// Gets or sets the operation payload.
        /// </summary>
        [JsonProperty("operation")]
        public WebHookOperationPayload Operation { get; set; }

        #endregion
    }
}
