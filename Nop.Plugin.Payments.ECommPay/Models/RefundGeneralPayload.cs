using Newtonsoft.Json;

namespace Nop.Plugin.Payments.ECommPay.Models
{
    /// <summary>
    /// Represents a general payload of the refund request
    /// </summary>
    public class RefundGeneralPayload
    {
        #region Properties

        /// <summary>
        /// Gets or sets the project id.
        /// </summary>
        [JsonProperty("project_id")]
        public int ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the payment id.
        /// </summary>
        [JsonProperty("payment_id")]
        public string PaymentId { get; set; }

        /// <summary>
        /// Gets or sets the signature.
        /// </summary>
        [JsonProperty("signature")]
        public string Signature { get; set; }

        #endregion
    }
}
