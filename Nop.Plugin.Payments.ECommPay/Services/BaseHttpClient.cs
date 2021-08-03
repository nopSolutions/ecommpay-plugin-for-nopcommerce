using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Plugin.Payments.Ecommpay.Models;

namespace Nop.Plugin.Payments.Ecommpay.Services
{
    /// <summary>
    /// Provides an abstraction for the HTTP client to interact with the endpoint.
    /// </summary>
    public abstract class BaseHttpClient
    {
        #region Fields

        private HttpClient _httpClient;
        private JsonSerializerSettings _defaultWriteSettings;


        #endregion

        #region Ctor

        public BaseHttpClient(HttpClient httpClient)
        {
            // set default settings
            httpClient.BaseAddress = new Uri(Defaults.ECommPay.API.Host);
            httpClient.Timeout = TimeSpan.FromSeconds(Defaults.ECommPay.API.DefaultTimeout);
            httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, Defaults.ECommPay.API.UserAgent);
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "*/*");
            _httpClient = httpClient;

            _defaultWriteSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            };
        }

        #endregion

        #region Methods

        protected virtual Task<TResponse> GetAsync<TResponse>(string requestUri, [CallerMemberName] string callerName = "")
        {
            return CallAsync<TResponse>(() => _httpClient.GetAsync(requestUri), callerName);
        }

        protected virtual async Task<TResponse> PostAsync<TResponse>(string requestUri, object request = null, [CallerMemberName] string callerName = "")
        {
            HttpContent body = null;
            if (request != null)
            {
                var content = JsonConvert.SerializeObject(request, _defaultWriteSettings);
                body = new StringContent(content, Encoding.UTF8, MimeTypes.ApplicationJson);
            }

            return await CallAsync<TResponse>(() => _httpClient.PostAsync(requestUri, body), callerName);
        }

        protected virtual async Task<TResponse> CallAsync<TResponse>(Func<Task<HttpResponseMessage>> requestFunc, [CallerMemberName] string callerName = "")
        {
            HttpResponseMessage response = null;
            try
            {
                response = await requestFunc();
            }
            catch (Exception exception)
            {
                throw new ApiException(500, $"Error when calling '{callerName}'. HTTP status code - 500. {exception.Message}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var statusCode = (int)response.StatusCode;
            if (statusCode >= 400)
            {
                // throw exception with deserialized error
                var errorResponse = JsonConvert.DeserializeObject<ApiError>(responseContent);
                var message = $"Error when calling '{callerName}'. HTTP status code - {statusCode}. ";
                if (errorResponse != null)
                {
                    message += @$"
                            Status - '{errorResponse.Status}'.
                            Code - '{errorResponse.Code}'.
                            Message - '{errorResponse.Message}'.";
                }

                throw new ApiException(statusCode, message, errorResponse);
            }

            return JsonConvert.DeserializeObject<TResponse>(responseContent);
        }

        #endregion
    }
}
