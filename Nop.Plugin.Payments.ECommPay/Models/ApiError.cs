using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Ecommpay.Models
{
    /// <summary>
    /// Represents a API error.
    /// </summary>
    public class ApiError
    {
        #region Properties

        /// <summary>
        /// Gets or sets the error status.
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the message instance.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        #endregion
    }
}
