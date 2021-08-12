using Nop.Core.Configuration;
using Nop.Plugin.Payments.Ecommpay.Domain;

namespace Nop.Plugin.Payments.Ecommpay
{
    /// <summary>
    /// Represents the settings of ECOMMPAY payment plugin
    /// </summary>
    public class EcommpayPaymentSettings : ISettings
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether to sandbox environment is active
        /// </summary>
        public bool IsTestMode { get; set; }

        /// <summary>
        /// Gets or sets a production project ID
        /// </summary>
        public int ProductionProjectId { get; set; }

        /// <summary>
        /// Gets or sets a production secret key 
        /// </summary>
        public string ProductionSecretKey { get; set; }

        /// <summary>
        /// Gets or sets the additional parameters to pass to the ECOMMPAY
        /// </summary>
        public string AdditionalParameters { get; set; }

        /// <summary>
        /// Gets or sets a payment flow type
        /// </summary>
        public PaymentFlowType PaymentFlowType { get; set; }

        /// <summary>
        /// Gets or sets an additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }

        #endregion
    }
}
