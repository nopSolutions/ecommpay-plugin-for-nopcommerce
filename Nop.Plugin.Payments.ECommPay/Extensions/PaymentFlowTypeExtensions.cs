using System;
using Nop.Plugin.Payments.Ecommpay.Domain;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.Ecommpay.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="PaymentFlowType"/>
    /// </summary>
    public static class PaymentFlowTypeExtensions
    {
        #region Methods

        /// <summary>
        /// Converts the <see cref="PaymentFlowType"/> to the <see cref="PaymentMethodType"/>
        /// </summary>
        /// <param name="paymentFlowType">The payment flow type</param>
        /// <returns>The value of the <see cref="PaymentMethodType"/></returns>
        public static PaymentMethodType ToPaymentMethodType(this PaymentFlowType paymentFlowType)
        {
            return paymentFlowType switch
            {
                PaymentFlowType.Iframe => PaymentMethodType.Standard,
                PaymentFlowType.NewBrowserTab => PaymentMethodType.Redirection,
                _ => throw new InvalidOperationException($"Cannot convert '{nameof(PaymentFlowType)}.{paymentFlowType}' to '{nameof(PaymentMethodType)}'.")
            };
        }

        #endregion
    }
}
