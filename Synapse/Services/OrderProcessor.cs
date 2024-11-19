using Synapse.Entities;
using Synapse.Interfaces;

namespace Synapse.Services
{
    public class OrderProcessor : IOrderProcessor
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IAlertService _alertService;
        private readonly ILogger<OrderProcessor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderProcessor"/> class.
        /// </summary>
        /// <param name="orderRepository">The order repository.</param>
        /// <param name="alertService">The alert service.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
        public OrderProcessor(
            IOrderRepository orderRepository,
            IAlertService alertService,
            ILogger<OrderProcessor> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes all orders by fetching them from the repository, processing each order, and updating the repository.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ProcessOrders()
        {
            IEnumerable<Order> orders = await _orderRepository.FetchOrders();

            foreach (Order order in orders)
            {
                Order processedOrder = await ProcessOrder(order);
                await _orderRepository.UpdateOrder(processedOrder);
            }
        }

        /// <summary>
        /// Processes a single order by sending alerts for delivered items and updating the order items.
        /// </summary>
        /// <param name="order">The order to process.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the processed order.</returns>
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

        /// <summary>
        /// Determines whether the specified order item is delivered.
        /// </summary>
        /// <param name="item">The order item to check.</param>
        /// <returns><c>true</c> if the specified item is delivered; otherwise, <c>false</c>.</returns>
        private static bool IsDelivered(OrderItem item)
        {
            return item.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase);
        }
    }
}
