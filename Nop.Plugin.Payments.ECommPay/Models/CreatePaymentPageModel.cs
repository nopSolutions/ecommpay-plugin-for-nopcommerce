using System.Collections.Generic;
using Microsoft.AspNetCore.WebUtilities;

namespace Nop.Plugin.Payments.Ecommpay.Models
{
    /// <summary>
    /// Represents the model to create the ECOMMPAY Payment Page
    /// </summary>
    public class CreatePaymentPageModel
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the creation of the model was successful
        /// </summary>
        public bool Success => Errors == null || Errors.Count == 0;

        /// <summary>
        /// Gets or sets the errors
        /// </summary>
        public IList<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the ECOMMPAY API base URL
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the payment query
        /// </summary>
        public IDictionary<string, string> Query { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets the combined URL
        /// </summary>
        public string Url => QueryHelpers.AddQueryString(BaseUrl, Query);

        #endregion
    }
}
