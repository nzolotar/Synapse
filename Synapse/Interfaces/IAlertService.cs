using Synapse.Entities;

namespace Synapse.Interfaces
{
    /// <summary>
    /// Defines the contract for an alert service that sends alerts based on order information.
    /// </summary>
    public interface IAlertService
    {
        /// <summary>
        /// Sends an alert for a specified order and optional order item.
        /// </summary>
        /// <param name="orderId">The ID of the order for which the alert is to be sent.</param>
        /// <param name="item">The optional order item associated with the alert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SendAlert(string orderId, OrderItem? item);
    }
}
