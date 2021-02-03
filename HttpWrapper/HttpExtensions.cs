using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpWrapper
{
    // Inspired By
    // https://johnthiriet.com/efficient-api-calls/
    // https://www.stevejgordon.co.uk/using-httpcompletionoption-responseheadersread-to-improve-httpclient-performance-dotnet
    public static class HttpClientExtensions
    {
        #region GetJsonAsync
        public static Task<TResponse> Get<TRequestObject, TResponse>(this HttpClient httpClient,
            string requestUrl,
            TRequestObject value = default,
            Func<HttpResponseMessage, Task> exceptionTranslatorDelegate = null,
            CancellationToken cancellationToken = default)
        {
            return DeserializeFromStreamCallAsync<TRequestObject, TResponse>(
                httpClient,
                HttpMethod.Get,
                requestUrl,
                value,
                exceptionTranslatorDelegate,
                cancellationToken);
        }

        public static Task<TResponse> Get<TResponse>(this HttpClient httpClient,
            string requestUrl,
            Func<HttpResponseMessage, Task> exceptionTranslatorDelegate = null,
            CancellationToken cancellationToken = default)
        {
            return DeserializeFromStreamCallAsync<TResponse>(
                httpClient,
                HttpMethod.Get,
                requestUrl,
                exceptionTranslatorDelegate,
                cancellationToken);
        }

        #endregion

        #region PutJsonAsync
        public static Task<TResponse> Put<TRequestObject, TResponse>(this HttpClient httpClient,
            string requestUrl,
            TRequestObject request,
            Func<HttpResponseMessage, Task> exceptionTranslatorDelegate = null,
            CancellationToken cancellationToken = default)
        {
            return DeserializeFromStreamCallAsync<TRequestObject, TResponse>(
                httpClient,
                HttpMethod.Put,
                requestUrl,
                request,
                exceptionTranslatorDelegate,
                cancellationToken);
        }

        public static Task Put<TRequestObject>(this HttpClient httpClient,
            string requestUrl,
            TRequestObject request,
            Func<HttpResponseMessage, Task> exceptionTranslatorDelegate = null,
            CancellationToken cancellationToken = default)
        {
            return DoNotExpectResponseThrowErrorIfFailure<TRequestObject>(
                httpClient,
                HttpMethod.Put,
                requestUrl,
                request,
                exceptionTranslatorDelegate,
                cancellationToken);
        }
        #endregion

        #region DeleteJsonAsync
        public static Task<TResponse> Delete<TRequestObject, TResponse>(this HttpClient httpClient,
            string requestUrl,
            TRequestObject request,
            Func<HttpResponseMessage, Task> exceptionTranslatorDelegate = null,
            CancellationToken cancellationToken = default)
        {
            return DeserializeFromStreamCallAsync<TRequestObject, TResponse>(
                httpClient,
                HttpMethod.Delete,
                requestUrl,
                request,
                exceptionTranslatorDelegate,
                cancellationToken);
        }

        public static Task<TResponse> Delete<TResponse>(this HttpClient httpClient,
            string requestUrl,
            Func<HttpResponseMessage, Task> exceptionTranslatorDelegate = null,
            CancellationToken cancellationToken = default)
        {
            return DeserializeFromStreamCallAsync<TResponse>(
                httpClient,
                HttpMethod.Delete,
                requestUrl,
                exceptionTranslatorDelegate,
                cancellationToken);
        }

        #endregion

        #region PostJsonAsync

        public static Task<TResponse> Post<TRequestObject, TResponse>(this HttpClient httpClient,
            string requestUrl,
            TRequestObject request,
            Func<HttpResponseMessage, Task> exceptionTranslatorDelegate = null,
            CancellationToken cancellationToken = default)
        {
            return DeserializeFromStreamCallAsync<TRequestObject, TResponse>(
                httpClient,
                HttpMethod.Post,
                requestUrl,
                request,
                exceptionTranslatorDelegate,
                cancellationToken);
        }

        public static Task Post<TRequestObject>(this HttpClient httpClient,
            string requestUrl,
            TRequestObject request,
            Func<HttpResponseMessage, Task> exceptionTranslatorDelegate = null,
            CancellationToken cancellationToken = default)
        {
            return DoNotExpectResponseThrowErrorIfFailure(
                httpClient,
                HttpMethod.Post,
                requestUrl,
                request,
                exceptionTranslatorDelegate,
                cancellationToken);
        }

        public static Task<TResponse> Post<TResponse>(this HttpClient httpClient,
            string requestUrl,
            Func<HttpResponseMessage, Task> exceptionTranslatorDelegate = null,
            CancellationToken cancellationToken = default)
        {
            return DeserializeFromStreamCallAsync<TResponse>(
                httpClient,
                HttpMethod.Post,
                requestUrl,
                exceptionTranslatorDelegate,
                cancellationToken);
        }


        public static async Task<TResponse> PostFormUrlEncodedAsync<TResponse>(
            this HttpClient httpClient,
            string requestUrl,
            NameValueCollection nameValueCollection,
            Func<HttpResponseMessage, Task> exceptionTranslatorDelegate = null,
            CancellationToken cancellationToken = default)
        {
            requestUrl = WellFormedRequestUri(httpClient.BaseAddress, requestUrl);

            var formDataKeyValuePairs = nameValueCollection
                .AllKeys
                .Select(key => new KeyValuePair<string, string>(key, nameValueCollection[key])).ToList();

            using var httpResponseMessage = await httpClient
                .PostAsync(requestUrl, new FormUrlEncodedContent(formDataKeyValuePairs), cancellationToken)
                .ConfigureAwait(false);

            if (exceptionTranslatorDelegate != null)
                await exceptionTranslatorDelegate(httpResponseMessage);

            var stream = await httpResponseMessage.Content.ReadAsStreamAsync();

            if (httpResponseMessage.IsSuccessStatusCode)
                return stream.DeserializeJsonFromStream<TResponse>();

            var content = await stream.StreamToStringAsync();

            throw new ExternalApiException(
                httpResponseMessage.StatusCode,
                content);
        }

        public static async Task<TResponse> PostMultipartAsync<TResponse>(
            this HttpClient httpClient,
            string requestUrl,
            MultipartFormDataContent content,
            Func<HttpResponseMessage, Task> exceptionTranslatorDelegate = null,
            CancellationToken cancellationToken = default)
        {
            requestUrl = WellFormedRequestUri(httpClient.BaseAddress, requestUrl);

            using var httpResponseMessage = await httpClient
                .PostAsync(requestUrl, content, cancellationToken)
                .ConfigureAwait(false);

            var stream = await httpResponseMessage.Content.ReadAsStreamAsync();

            if (exceptionTranslatorDelegate != null)
                await exceptionTranslatorDelegate(httpResponseMessage);

            if (httpResponseMessage.IsSuccessStatusCode)
                return stream.DeserializeJsonFromStream<TResponse>();

            var response = await stream.StreamToStringAsync();

            throw new ExternalApiException(
                httpResponseMessage.StatusCode,
                response);
        }

        #endregion
        private static async Task<TResponse> DeserializeFromStreamCallAsync<TRequestObject, TResponse>(
            HttpClient httpClient,
            HttpMethod method,
            string requestUrl,
            TRequestObject request = default,
            Func<HttpResponseMessage, Task> exceptionTranslatorDelegate = null,
            CancellationToken cancellationToken = default)
        {
            requestUrl = WellFormedRequestUri(httpClient.BaseAddress, requestUrl);

            using var httpRequestMessage = new HttpRequestMessage(method, requestUrl);

            using var httpContent = CreateHttpContent(request);
            httpRequestMessage.Content = httpContent;

            using var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var stream = await httpResponseMessage.Content.ReadAsStreamAsync();

            if (exceptionTranslatorDelegate != null)
                await exceptionTranslatorDelegate(httpResponseMessage);

            if (httpResponseMessage.IsSuccessStatusCode)
                return stream.DeserializeJsonFromStream<TResponse>();

            var content = await stream.StreamToStringAsync();

            throw new ExternalApiException(
                httpResponseMessage.StatusCode,
                content);
        }

        private static async Task<TResponse> DeserializeFromStreamCallAsync<TResponse>(
            HttpClient httpClient,
            HttpMethod method,
            string requestUrl,
            Func<HttpResponseMessage, Task> exceptionTranslatorDelegate = null,
            CancellationToken cancellationToken = default)
        {
            requestUrl = WellFormedRequestUri(httpClient.BaseAddress, requestUrl);

            using var httpRequestMessage = new HttpRequestMessage(method, requestUrl);

            using var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (exceptionTranslatorDelegate != null)
                await exceptionTranslatorDelegate(httpResponseMessage);

            var stream = await httpResponseMessage.Content.ReadAsStreamAsync();

            if (httpResponseMessage.IsSuccessStatusCode)
                return stream.DeserializeJsonFromStream<TResponse>();

            var content = await stream.StreamToStringAsync();

            throw new ExternalApiException(
                httpResponseMessage.StatusCode,
                content);
        }

        private static async Task DoNotExpectResponseThrowErrorIfFailure<TRequestObject>(
            HttpClient httpClient,
            HttpMethod method,
            string requestUrl,
            TRequestObject request = default,
            Func<HttpResponseMessage, Task> exceptionTranslatorDelegate = null,
            CancellationToken cancellationToken = default)
        {
            requestUrl = WellFormedRequestUri(httpClient.BaseAddress, requestUrl);
            using var httpRequestMessage = new HttpRequestMessage(method, requestUrl);

            using var httpContent = CreateHttpContent(request);
            httpRequestMessage.Content = httpContent;

            using var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (exceptionTranslatorDelegate != null)
                await exceptionTranslatorDelegate(httpResponseMessage);

            var stream = await httpResponseMessage.Content.ReadAsStreamAsync();

            if (httpResponseMessage.IsSuccessStatusCode)
                return;

            var content = await stream.StreamToStringAsync();

            throw new ExternalApiException(
                httpResponseMessage.StatusCode,
                content);
        }

        #region Private Helpers

        private static HttpContent CreateHttpContent<T>(T content)
        {
            if (content == null)
                return null;

            var ms = new MemoryStream();
            ms.SerializeJsonIntoStream(content);

            ms.Seek(0, SeekOrigin.Begin);
            var httpContent = new StreamContent(ms);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return httpContent;
        }

        private static string WellFormedRequestUri(Uri baseAddress, string requestUrl)
        {
            if (baseAddress.ToString().EndsWith("/") && requestUrl.StartsWith("/"))
            {
                requestUrl = requestUrl.Substring(1);
            }

            if (!baseAddress.ToString().EndsWith("/") && !requestUrl.StartsWith("/"))
            {
                requestUrl = $"/{requestUrl}";
            }

            return requestUrl;
        }

        #endregion
    }
}
