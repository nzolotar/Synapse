namespace Synapse.Interfaces
{
    /// <summary>
    /// Defines a wrapper for HTTP client operations.
    /// </summary>
    public interface IHttpClientWrapper
    {
        /// <summary>
        /// Sends a GET request to the specified URL.
        /// </summary>
        /// <param name="url">The URL to send the GET request to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message.</returns>
        Task<HttpResponseMessage> GetAsync(string url);

        /// <summary>
        /// Sends a POST request to the specified URL with the provided content.
        /// </summary>
        /// <param name="url">The URL to send the POST request to.</param>
        /// <param name="content">The content to send in the POST request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message.</returns>
        Task<HttpResponseMessage> PostAsync(string url, StringContent content);
    }
}
