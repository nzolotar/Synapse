using Synapse.Entities;

namespace Synapse.Interfaces
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> FetchOrders();
        Task UpdateOrder(Order order);
    }
}
