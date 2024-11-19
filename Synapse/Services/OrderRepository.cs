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

        public OrderRepository(IHttpClientWrapper httpClient, string ordersApiUrl, string updateApiUrl, ILogger<OrderRepository> logger)
        {
            _httpClient = httpClient;
            _ordersApiUrl = ordersApiUrl;
            _updateApiUrl = updateApiUrl;
            _logger = logger;
        }

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
