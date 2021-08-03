using FluentValidation;
using Nop.Plugin.Payments.Ecommpay.Areas.Admin.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Payments.Ecommpay.Areas.Admin.Validators
{
    /// <summary>
    /// Represents a validator for <see cref="ConfigurationModel"/>
    /// </summary>
    public class ConfigurationModelValidator : BaseNopValidator<ConfigurationModel>
    {
        #region Ctor

        public ConfigurationModelValidator(ILocalizationService localizationService)
        {
            RuleFor(model => model.TestProjectId)
                .NotEmpty()
                .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.Ecommpay.Fields.TestProjectId.Required"))
                .When(model => model.IsTestMode);

            RuleFor(model => model.TestProjectId)
                .Must(value => int.TryParse(value, out _))
                .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.Ecommpay.Fields.TestProjectId.ShouldBeNumeric"))
                .When(model => model.IsTestMode);

            RuleFor(model => model.TestSecretKey)
                .NotEmpty()
                .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.Ecommpay.Fields.TestSecretKey.Required"))
                .When(model => model.IsTestMode);

            RuleFor(model => model.ProductionProjectId)
                .NotEmpty()
                .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.Ecommpay.Fields.ProductionProjectId.Required"))
                .When(model => !model.IsTestMode);

            RuleFor(model => model.ProductionProjectId)
                .Must(value => int.TryParse(value, out _))
                .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.Ecommpay.Fields.ProductionProjectId.ShouldBeNumeric"))
                .When(model => !model.IsTestMode);

            RuleFor(model => model.ProductionSecretKey)
                .NotEmpty()
                .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.Ecommpay.Fields.ProductionSecretKey.Required"))
                .When(model => !model.IsTestMode);
        }

        #endregion
    }
}
