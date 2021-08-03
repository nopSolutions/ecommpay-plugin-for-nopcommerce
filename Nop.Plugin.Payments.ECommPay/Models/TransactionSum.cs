using Newtonsoft.Json;

namespace Nop.Plugin.Payments.ECommPay.Models
{
    /// <summary>
    /// Represents a transaction sum
    /// </summary>
    public class TransactionSum
    {
        #region Properties

        /// <summary>
        /// Gets or sets the amount.
        /// </summary>
        [JsonProperty("amount")]
        public int Amount { get; set; }

        /// <summary>
        /// Gets or sets the currency code.
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        #endregion
    }
}
