using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.Ecommpay.Areas.Admin.Models
{
    /// <summary>
    /// Represents a plugin configuration model.
    /// </summary>
    public record ConfigurationModel : BaseNopModel
    {
        #region Properties

        public int ActiveStoreScopeConfiguration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to sandbox environment is active
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.Ecommpay.Fields.IsTestMode")]
        public bool IsTestMode { get; set; }
        public bool IsTestMode_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets a production project ID
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.Ecommpay.Fields.ProductionProjectId")]
        public string ProductionProjectId { get; set; }
        public bool ProductionProjectId_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets a production secret key 
        /// </summary>
        [DataType(DataType.Password)]
        [NopResourceDisplayName("Plugins.Payments.Ecommpay.Fields.ProductionSecretKey")]
        public string ProductionSecretKey { get; set; }
        public bool ProductionSecretKey_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets a payment flow type ID
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.Ecommpay.Fields.PaymentFlowTypeId")]
        public int PaymentFlowTypeId { get; set; }
        public bool PaymentFlowTypeId_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets the available payment flow types
        /// </summary>
        public IList<SelectListItem> AvailablePaymentFlowTypes { get; set; }

        /// <summary>
        /// Gets or sets the additional parameters to pass to the ECOMMPAY
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.Ecommpay.Fields.AdditionalParameters")]
        public string AdditionalParameters { get; set; }
        public bool AdditionalParameters_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets an additional fee
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.Ecommpay.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.Ecommpay.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Ecommpay.Fields.CallbackEndpoint")]
        public string CallbackEndpoint { get; set; }

        #endregion

        #region Ctor

        public ConfigurationModel()
        {
            AvailablePaymentFlowTypes = new List<SelectListItem>();
        }

        #endregion
    }
}
