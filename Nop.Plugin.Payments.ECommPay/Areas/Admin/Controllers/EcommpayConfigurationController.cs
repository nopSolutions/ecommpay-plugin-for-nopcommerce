using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Plugin.Payments.Ecommpay.Areas.Admin.Models;
using Nop.Plugin.Payments.Ecommpay.Domain;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Ecommpay.Areas.Admin.Controllers
{
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    [AuthorizeAdmin]
    public class EcommpayConfigurationController : BasePluginController
    {
        #region Fields

        private readonly IPermissionService _permissionService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;

        #endregion

        #region Ctor

        public EcommpayConfigurationController(
            IPermissionService permissionService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            ISettingService settingService,
            IStoreContext storeContext,
            IStoreService storeService
        )
        {
            _permissionService = permissionService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _settingService = settingService;
            _storeContext = storeContext;
            _storeService = storeService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Configures the plugin in admin area.
        /// </summary>
        /// <returns>The view to configure.</returns>
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var ecommpayPaymentSettings = await _settingService.LoadSettingAsync<EcommpayPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                ActiveStoreScopeConfiguration = storeScope,
                IsTestMode = ecommpayPaymentSettings.IsTestMode,
                TestProjectId = ecommpayPaymentSettings.TestProjectId.ToString(),
                TestSecretKey = ecommpayPaymentSettings.TestSecretKey,
                ProductionProjectId = ecommpayPaymentSettings.ProductionProjectId.ToString(),
                ProductionSecretKey = ecommpayPaymentSettings.ProductionSecretKey,
                PaymentFlowTypeId = (int)ecommpayPaymentSettings.PaymentFlowType,
                AdditionalParameterSystemNames = ecommpayPaymentSettings.AdditionalParameterSystemNames,
                AdditionalFee = ecommpayPaymentSettings.AdditionalFee,
                AdditionalFeePercentage = ecommpayPaymentSettings.AdditionalFeePercentage,
            };

            var store = storeScope > 0
                ? await _storeService.GetStoreByIdAsync(storeScope)
                : await _storeContext.GetCurrentStoreAsync();
            model.CallbackEndpoint = $"{store.Url.TrimEnd('/')}{Url.RouteUrl(Defaults.CallbackRouteName)}".ToLowerInvariant();

            var availablePaymentFlowTypes = await ecommpayPaymentSettings.PaymentFlowType.ToSelectListAsync();
            foreach (var paymentFlowType in availablePaymentFlowTypes)
                model.AvailablePaymentFlowTypes.Add(paymentFlowType);

            foreach (var parameter in Defaults.ECommPay.AdditionalParameters.All)
                model.AvailableAdditionalParameterSystemNames.Add(new SelectListItem(await _localizationService.GetResourceAsync(parameter.LocaleName), parameter.SystemName.ToString()));

            if (storeScope > 0)
            {
                model.IsTestMode_OverrideForStore = await _settingService.SettingExistsAsync(ecommpayPaymentSettings, x => x.IsTestMode, storeScope);
                model.TestProjectId_OverrideForStore = await _settingService.SettingExistsAsync(ecommpayPaymentSettings, x => x.TestProjectId, storeScope);
                model.TestSecretKey_OverrideForStore = await _settingService.SettingExistsAsync(ecommpayPaymentSettings, x => x.TestSecretKey, storeScope);
                model.ProductionProjectId_OverrideForStore = await _settingService.SettingExistsAsync(ecommpayPaymentSettings, x => x.ProductionProjectId, storeScope);
                model.ProductionSecretKey_OverrideForStore = await _settingService.SettingExistsAsync(ecommpayPaymentSettings, x => x.ProductionSecretKey, storeScope);
                model.PaymentFlowTypeId_OverrideForStore = await _settingService.SettingExistsAsync(ecommpayPaymentSettings, x => x.PaymentFlowType, storeScope);
                model.AdditionalParameterSystemNames_OverrideForStore = await _settingService.SettingExistsAsync(ecommpayPaymentSettings, x => x.AdditionalParameterSystemNames, storeScope);
                model.AdditionalFee_OverrideForStore = await _settingService.SettingExistsAsync(ecommpayPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = await _settingService.SettingExistsAsync(ecommpayPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            }

            return View("~/Plugins/Payments.Ecommpay/Areas/Admin/Views/Configure.cshtml", model);
        }

        /// <summary>
        /// Configures the plugin in admin area.
        /// </summary>
        /// <param name="model">The configuration model.</param>
        /// <returns>The view to configure.</returns>
        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var ecommpayPaymentSettings = await _settingService.LoadSettingAsync<EcommpayPaymentSettings>(storeScope);

            //save settings
            ecommpayPaymentSettings.IsTestMode = model.IsTestMode;
            ecommpayPaymentSettings.TestProjectId = int.Parse(model.TestProjectId);
            ecommpayPaymentSettings.TestSecretKey = model.TestSecretKey;
            ecommpayPaymentSettings.ProductionProjectId = int.Parse(model.ProductionProjectId);
            ecommpayPaymentSettings.ProductionSecretKey = model.ProductionSecretKey;
            ecommpayPaymentSettings.PaymentFlowType = (PaymentFlowType)model.PaymentFlowTypeId;
            ecommpayPaymentSettings.AdditionalParameterSystemNames = model.AdditionalParameterSystemNames.ToList();
            ecommpayPaymentSettings.AdditionalFee = model.AdditionalFee;
            ecommpayPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(ecommpayPaymentSettings, x => x.IsTestMode, model.IsTestMode_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(ecommpayPaymentSettings, x => x.TestProjectId, model.TestProjectId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(ecommpayPaymentSettings, x => x.TestSecretKey, model.TestSecretKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(ecommpayPaymentSettings, x => x.ProductionProjectId, model.ProductionProjectId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(ecommpayPaymentSettings, x => x.ProductionSecretKey, model.ProductionSecretKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(ecommpayPaymentSettings, x => x.PaymentFlowType, model.PaymentFlowTypeId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(ecommpayPaymentSettings, x => x.AdditionalParameterSystemNames, model.AdditionalParameterSystemNames_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(ecommpayPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(ecommpayPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return RedirectToAction("Configure");
        }

        #endregion
    }
}
