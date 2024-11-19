using Synapse.Entities;
using Synapse.Interfaces;

namespace Synapse.Services
{
    public class OrderProcessor : IOrderProcessor
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IAlertService _alertService;
        private readonly ILogger<OrderProcessor> _logger;

        public OrderProcessor(
            IOrderRepository orderRepository,
            IAlertService alertService,
            ILogger<OrderProcessor> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessOrders()
        {
            IEnumerable<Order> orders = await _orderRepository.FetchOrders();

            foreach (Order order in orders)
            {
                Order processedOrder = await ProcessOrder(order);
                await _orderRepository.UpdateOrder(processedOrder);
            }
        }

        private async Task<Order> ProcessOrder(Order order)
        {
            List<OrderItem> updatedItems = [];

            foreach (OrderItem item in order.Items)
            {
                if (IsDelivered(item))
                {
                    try
                    {
                        await _alertService.SendAlert(order.OrderId, item);
                        OrderItem updatedItem = item with { DeliveryNotification = item.DeliveryNotification + 1 };
                        updatedItems.Add(updatedItem);
                        _logger.LogInformation("Successfully processed alert for order {OrderId}, item {ItemDescription}",
                            order.OrderId, item.Description);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send alert for order {OrderId}, item {ItemDescription}",
                            order.OrderId, item.Description);
                        continue;
                    }
                }
                else
                {
                    updatedItems.Add(item);
                }
            }

            return order with { Items = updatedItems };
        }

        private static bool IsDelivered(OrderItem item)
        {
            return item.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase);
        }
    }
}
