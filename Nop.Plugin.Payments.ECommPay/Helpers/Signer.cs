using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nop.Plugin.Payments.ECommPay.Helpers
{
    /// <summary>
    /// Represents an helper class to sign the data for the ECommPay requests.
    /// </summary>
    public static class Signer
    {
        #region Methods

        /// <summary>
        /// Validates the payload by the specified secret key.
        /// </summary>
        /// <param name="key">The secret key.</param>
        /// <param name="payload">The payload in JSON format.</param>
        /// <returns>The value indicating whether the payload is signed correctly.</returns>
        public static bool ValidateSignature(string key, string payload)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (string.IsNullOrWhiteSpace(payload))
                return false;

            var parametersToSign = GetParametersFromJson(payload);
            if (parametersToSign == null)
                return false;

            if (!parametersToSign.TryGetValue("signature", out var actualSignature))
                return false;

            parametersToSign.Remove("signature");

            var expectedSignature = GenerateSignature(key, parametersToSign);

            return actualSignature == expectedSignature;
        }

        /// <summary>
        /// Generates the signature by the specified secret key and parameters.
        /// </summary>
        /// <param name="key">The secret key.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The signature.</returns>
        public static string GenerateSignature(string key, IDictionary<string, string> parameters)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));

            var orderedParameters = parameters
                .Select(p => $"{p.Key}:{p.Value}")
                .OrderBy(value => value);

            var payload = string.Join(";", orderedParameters);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var keyBytes = Encoding.UTF8.GetBytes(key);

            using var cryptographer = new HMACSHA512(keyBytes);
            var hashBytes = cryptographer.ComputeHash(payloadBytes);

            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Generates the signature by the specified secret key and object.
        /// </summary>
        /// <param name="key">The secret key.</param>
        /// <param name="obj">The object.</param>
        /// <returns>The signature.</returns>
        public static string GenerateSignature(string key, object obj)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (obj is null)
                throw new ArgumentNullException(nameof(obj));

            var payload = JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            });
            var parameters = GetParametersFromJson(payload);
            if (parameters == null)
                return null;

            if (parameters.Keys.Any(key => key.Contains("signature")))
            {
                var signatureKey = parameters.Keys.First(key => key.Contains("signature"));
                parameters.Remove(signatureKey);
            }

            return GenerateSignature(key, parameters);
        }

        #endregion

        #region Utilities

        private static IDictionary<string, string> GetParametersFromJson(string json)
        {
            JObject jObject = null;
            try
            {
                jObject = JObject.Parse(json);
            }
            catch (JsonReaderException)
            {
                return null;
            }

            if (!jObject.HasValues)
                return null;

            var parameters = new Dictionary<string, string>();
            foreach (var token in jObject)
                AddParameter(token.Key, token.Value, parameters);

            return parameters;
        }

        private static void AddParameter(string name, JToken jValue, IDictionary<string, string> allParameters)
        {
            switch (jValue.Type)
            {
                case JTokenType.Object:
                    foreach (var token in jValue as JObject)
                        AddParameter($"{name}:{token.Key}", token.Value, allParameters);
                    break;

                case JTokenType.Array:
                    if (!jValue.HasValues)
                        break;

                    var jArray = jValue as JArray;
                    for (var i = 0; i < jArray.Count; i++)
                        AddParameter($"{name}:{i}", jArray[i], allParameters);

                    break;

                case JTokenType.TimeSpan:
                case JTokenType.Uri:
                case JTokenType.Guid:
                case JTokenType.Raw:
                case JTokenType.Bytes:
                case JTokenType.String:
                case JTokenType.Float:
                case JTokenType.Integer:
                    allParameters[name] = jValue.ToString();
                    break;

                case JTokenType.Boolean:
                    allParameters[name] = jValue.Value<bool>() ? "1" : "0";
                    break;

                case JTokenType.Date:
                    allParameters[name] = jValue.Value<DateTime>().ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss+0000");
                    break;

                case JTokenType.Null:
                    allParameters[name] = string.Empty;
                    break;

                default:
                    break;
            }
        }

        #endregion
    }
}
