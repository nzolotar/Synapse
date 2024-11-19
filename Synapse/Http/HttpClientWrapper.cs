using Synapse.Interfaces;

namespace Synapse.Http
{
    /// <summary>
    /// A wrapper class for HttpClient to facilitate HTTP requests.
    /// </summary>
    public class HttpClientWrapper : IHttpClientWrapper
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientWrapper"/> class.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance to be used for making HTTP requests.</param>
        public HttpClientWrapper(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Sends a GET request to the specified URL.
        /// </summary>
        /// <param name="url">The URL to which the GET request is sent.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message.</returns>
        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await _httpClient.GetAsync(url);
        }

        /// <summary>
        /// Sends a POST request to the specified URL with the provided content.
        /// </summary>
        /// <param name="url">The URL to which the POST request is sent.</param>
        /// <param name="content">The content to be sent in the POST request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message.</returns>
        public async Task<HttpResponseMessage> PostAsync(string url, StringContent content)
        {
            return await _httpClient.PostAsync(url, content);
        }
    }
}
