namespace Nop.Plugin.Payments.Ecommpay.Domain
{
    /// <summary>
    /// Represents an payment flow type
    /// </summary>
    public enum PaymentFlowType
    {
        /// <summary>
        /// Customer perform payment on ECOMMPAY side in new browser tab
        /// </summary>
        NewBrowserTab,

        /// <summary>
        /// Customer perform payment on merchant side with iframe
        /// </summary>
        Iframe,
    }
}
