using Synapse.Entities;

namespace Synapse.Interfaces
{
    /// <summary>
    /// Interface for order repository to handle order-related data operations.
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// Fetches all orders.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of orders.</returns>
        Task<IEnumerable<Order>> FetchOrders();

        /// <summary>
        /// Updates the specified order.
        /// </summary>
        /// <param name="order">The order to update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateOrder(Order order);
    }
}
