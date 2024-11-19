namespace Synapse.Entities
{
    /// <summary>
    /// Represents an order with an ID and a list of order items.
    /// </summary>
    /// <param name="OrderId">The unique identifier for the order.</param>
    /// <param name="Items">The list of items in the order.</param>
    public record Order(string OrderId, List<OrderItem> Items);
}
