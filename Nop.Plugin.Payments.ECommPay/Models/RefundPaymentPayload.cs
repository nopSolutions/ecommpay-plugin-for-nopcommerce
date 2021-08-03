using Newtonsoft.Json;

namespace Nop.Plugin.Payments.ECommPay.Models
{
    /// <summary>
    /// Represents a payment payload of the refund request
    /// </summary>
    public class RefundPaymentPayload
    {
        #region Properties

        /// <summary>
        /// Gets or sets the amount.
        /// </summary>
        [JsonProperty("amount")]
        public int? Amount { get; set; }

        /// <summary>
        /// Gets or sets the currency code.
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        #endregion
    }
}
