using Newtonsoft.Json;

namespace Nop.Plugin.Payments.ECommPay.Models
{
    /// <summary>
    /// Represents a request for the creation of a new refund
    /// </summary>
    public class CreateRefundRequest
    {
        #region Properties

        /// <summary>
        /// Gets or sets the general payload.
        /// </summary>
        [JsonProperty("general")]
        public RefundGeneralPayload General { get; set; }

        /// <summary>
        /// Gets or sets the payment payload.
        /// </summary>
        [JsonProperty("payment")]
        public RefundPaymentPayload Payment { get; set; }

        #endregion
    }
}
