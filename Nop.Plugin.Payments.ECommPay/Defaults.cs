using Nop.Core;

namespace Nop.Plugin.Payments.Ecommpay
{
    /// <summary>
    /// Represents a plugin defaults
    /// </summary>
    public static class Defaults
    {
        /// <summary>
        /// Gets the plugin system name
        /// </summary>
        public static string SystemName => "Payments.Ecommpay";

        /// <summary>
        /// Gets the session key to get process payment request
        /// </summary>
        public static string PaymentRequestSessionKey => "OrderPaymentInfo";

        /// <summary>
        /// Gets the plugin configuration route name
        /// </summary>
        public static string ConfigurationRouteName => "Plugin.Payments.Ecommpay.Configure";

        /// <summary>
        /// Gets the callback route name
        /// </summary>
        public static string CallbackRouteName => "EcommpayCallback";

        /// <summary>
        /// Gets the order details route name
        /// </summary>
        public static string OrderDetailsRouteName => "OrderDetails";
        
        /// <summary>
        /// Gets the checkout completed route name
        /// </summary>
        public static string CheckoutCompletedRouteName => "CheckoutCompleted";

        /// <summary>
        /// Gets the checkout payment info route name
        /// </summary>
        public static string CheckoutPaymentInfoRouteName => "CheckoutPaymentInfo";

        /// <summary>
        /// Gets the checkout one page route name
        /// </summary>
        public static string CheckoutOnePageRouteName => "CheckoutOnePage";

        /// <summary>
        /// Gets a name of the view component to display payment info in public store
        /// </summary>
        public const string PAYMENT_INFO_VIEW_COMPONENT_NAME = "EcommpayPaymentInfo";

        /// <summary>
        /// Gets a name of the view component to add the libraries links in public store
        /// </summary>
        public const string HEAD_LINKS_VIEW_COMPONENT_NAME = "EcommpayHeadLinks";

        /// <summary>
        /// Represents a ECOMMPAY defaults
        /// </summary>
        public static class ECommPay
        {
            /// <summary>
            /// Represents a additional parameters defaults
            /// </summary>
            public static class AdditionalParameters
            {
                /// <summary>
                /// Gets the all available parameters
                /// </summary>
                public static (string LocaleName, string SystemName)[] All => new[]
                {
                    Customer.PersonalData, 
                    Customer.BillingAddress
                };

                /// <summary>
                /// Represents a customer parameters defaults
                /// </summary>
                public static class Customer
                {
                    /// <summary>
                    /// Gets the personal data parameter
                    /// </summary>
                    public static (string LocaleName, string SystemName) PersonalData => ("Plugins.Payments.Ecommpay.AdditionalParameters.Customer.PersonalData", "customer.personaldata");

                    /// <summary>
                    /// Gets the billing address parameter
                    /// </summary>
                    public static (string LocaleName, string SystemName) BillingAddress => ("Plugins.Payments.Ecommpay.AdditionalParameters.Customer.BillingAddress", "customer.billingaddress");
                }
            }

            /// <summary>
            /// Represents a widget defaults
            /// </summary>
            public static class PaymentPage
            {
                /// <summary>
                /// Gets the Payment Page host
                /// </summary>
                public static string Host => "https://paymentpage.ecommpay.com";

                /// <summary>
                /// Gets the Payment Page URL
                /// </summary>
                public static string Url => Host + "/payment";

                /// <summary>
                /// Gets the CSS script URL for iframe or pop-up widgets
                /// </summary>
                public static string StylesUrl => Host + "/shared/merchant.css";

                /// <summary>
                /// Gets the JS script URL for iframe or pop-up widgets
                /// </summary>
                public static string ScriptUrl => Host + "/shared/merchant.js";

                /// <summary>
                /// Gets the iframe container name
                /// </summary>
                public static string ContainerName => "ecommpay-payment-container";
            }

            /// <summary>
            /// Represents a API defaults
            /// </summary>
            public static class API
            {
                /// <summary>
                /// Gets the host
                /// </summary>
                public static string Host => "https://api.ecommpay.com";

                /// <summary>
                /// Gets the user agent
                /// </summary>
                public static string UserAgent => $"nopCommerce-{NopVersion.FULL_VERSION}";

                /// <summary>
                /// Gets the default timeout
                /// </summary>
                public static int DefaultTimeout => 20;

                /// <summary>
                /// Represents a endpoints defaults
                /// </summary>
                public class Endpoints
                {
                    /// <summary>
                    /// Gets the refund
                    /// </summary>
                    public static string Refund => "/v2/payment/card/refund";
                }
            }
        }
    }
}
