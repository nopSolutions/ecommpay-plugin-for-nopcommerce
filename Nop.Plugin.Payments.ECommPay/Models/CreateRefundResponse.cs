using Newtonsoft.Json;

namespace Nop.Plugin.Payments.ECommPay.Models
{
    /// <summary>
    /// Represents a response when the refund of the transaction is requested
    /// </summary>
    public class CreateRefundResponse
    {
        #region Properties

        /// <summary>
        /// Gets or sets the request registration status.
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        #endregion
    }
}
