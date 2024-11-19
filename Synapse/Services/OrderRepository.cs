using Newtonsoft.Json.Linq;
using Synapse.Entities;
using Synapse.Interfaces;

namespace Synapse.Services
{
    public class OrderRepository : IOrderRepository
    {
        private readonly IHttpClientWrapper _httpClient;
        private readonly string _ordersApiUrl;
        private readonly string _updateApiUrl;
        private readonly ILogger<OrderRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderRepository"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client wrapper.</param>
        /// <param name="ordersApiUrl">The URL for fetching orders.</param>
        /// <param name="updateApiUrl">The URL for updating orders.</param>
        /// <param name="logger">The logger instance.</param>
        public OrderRepository(IHttpClientWrapper httpClient, string ordersApiUrl, string updateApiUrl, ILogger<OrderRepository> logger)
        {
            _httpClient = httpClient;
            _ordersApiUrl = ordersApiUrl;
            _updateApiUrl = updateApiUrl;
            _logger = logger;
        }

        /// <summary>
        /// Fetches the list of orders from the API.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable of orders.</returns>
        public async Task<IEnumerable<Order>> FetchOrders()
        {
            _logger.LogInformation("Fetching orders from {OrdersApiUrl}", _ordersApiUrl);

            HttpResponseMessage response = await _httpClient.GetAsync(_ordersApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                        "Failed to fetch orders. Status code: {StatusCode}, Reason: {ReasonPhrase}",
                        response.StatusCode,
                        response.ReasonPhrase);
                return Enumerable.Empty<Order>();
            }

            string ordersData = await response.Content.ReadAsStringAsync();
            Order[]? ordersArray = JArray.Parse(ordersData).ToObject<Order[]>();

            if (ordersArray == null || !ordersArray.Any())
            {
                _logger.LogInformation("No orders found in the response");
                return Enumerable.Empty<Order>();
            }
            else
            {
                _logger.LogInformation(
                        "Successfully fetched {OrderCount} orders. Order IDs: {@OrderIds}",
                        ordersArray.Length,
                        ordersArray.Select(o => o.OrderId));
                return ordersArray;
            }
        }

        /// <summary>
        /// Updates the specified order.
        /// </summary>
        /// <param name="order">The order to update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the order is null.</exception>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
        public async Task UpdateOrder(Order order)
        {
            if (order == null)
            {
                _logger.LogError("Attempted to update null order");
                throw new ArgumentNullException(nameof(order));
            }

            try
            {
                _logger.LogInformation(
                    "Updating order {OrderId} with {ItemCount} items",
                    order.OrderId,
                    order.Items.Count);

                StringContent content = new(
                JObject.FromObject(order).ToString(),
                System.Text.Encoding.UTF8,
                "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(_updateApiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = $"Failed to update order {order.OrderId}";
                    _logger.LogError(
                        "Update failed for order {OrderId}. Status code: {StatusCode}, Reason: {ReasonPhrase}",
                        order.OrderId,
                        response.StatusCode,
                        response.ReasonPhrase);
                    throw new HttpRequestException(errorMessage);
                }

                _logger.LogInformation(
                    "Successfully updated order {OrderId}",
                    order.OrderId);
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while updating order {OrderId}",
                    order.OrderId);
                throw;
            }
        }
    }
}
