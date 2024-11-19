namespace Synapse.Entities
{
    public record Order(string OrderId, List<OrderItem> Items);
}
