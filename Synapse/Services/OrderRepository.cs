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

        public OrderRepository(IHttpClientWrapper httpClient, string ordersApiUrl, string updateApiUrl)
        {
            _httpClient = httpClient;
            _ordersApiUrl = ordersApiUrl;
            _updateApiUrl = updateApiUrl;
        }

        public async Task<IEnumerable<Order>> FetchOrders()
        {
            HttpResponseMessage response = await _httpClient.GetAsync(_ordersApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<Order>();
            }

            string ordersData = await response.Content.ReadAsStringAsync();
            Order[]? ordersArray = JArray.Parse(ordersData).ToObject<Order[]>();
            return ordersArray ?? Enumerable.Empty<Order>();
        }

        public async Task UpdateOrder(Order order)
        {
            StringContent content = new(
                JObject.FromObject(order).ToString(),
                System.Text.Encoding.UTF8,
                "application/json");

            await _httpClient.PostAsync(_updateApiUrl, content);
        }
    }
}
