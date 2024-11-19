namespace Synapse.Entities
{
    /// <summary>
    /// Represents an item in an order.
    /// </summary>
    /// <param name="Description">The description of the order item.</param>
    /// <param name="Status">The status of the order item.</param>
    /// <param name="DeliveryNotification">The delivery notification status of the order item.</param>
    public record OrderItem(string Description, string Status, int DeliveryNotification);
}
