using Newtonsoft.Json;

namespace Nop.Plugin.Payments.ECommPay.Models
{
    /// <summary>
    /// Represents a operation payload of the web hook request
    /// </summary>
    public class WebHookOperationPayload
    {
        #region Properties

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the initial sum.
        /// </summary>
        [JsonProperty("sum_initial")]
        public TransactionSum InitialSum { get; set; }

        #endregion
    }
}
