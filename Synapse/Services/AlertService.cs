using Newtonsoft.Json;
using Synapse.Entities;
using Synapse.Interfaces;

namespace Synapse.Services
{
    public class AlertService : IAlertService
    {
        private readonly IHttpClientWrapper _httpClient;
        private readonly string _alertApiUrl;
        private readonly ILogger<AlertService> _logger;
        public AlertService(IHttpClientWrapper httpClient, string alertApiUrl,
            ILogger<AlertService> logger)
        {
            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));

            if (string.IsNullOrWhiteSpace(alertApiUrl))
                throw new ArgumentException("Alert API URL can not be blank");

            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _alertApiUrl = alertApiUrl;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendAlert(string orderId, OrderItem? item)
        {
            if (string.IsNullOrEmpty(orderId))
                throw new ArgumentException("OrderId cannot be null or empty");

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            _logger.LogInformation(
                "Sending alert for order {OrderId} with item {ItemDescription}",
                orderId,
                item.Description);

            var alertData = new
            {
                Message = $"Alert for delivered item: Order {orderId}, Item: {item.Description}, " +
                         $"Delivery Notifications: {item.DeliveryNotification}"
            };

            StringContent content = new(
                            JsonConvert.SerializeObject(alertData),
                            System.Text.Encoding.UTF8,
                            "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(_alertApiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                        "Failed to send alert for order {OrderId}. Status code: {StatusCode}",
                        orderId,
                        response.StatusCode);
                throw new HttpRequestException($"Failed to send alert for order {orderId}");
            }
        }
    }
}
