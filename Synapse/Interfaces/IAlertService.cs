using Synapse.Entities;

namespace Synapse.Interfaces
{
    public interface IAlertService
    {
        Task SendAlert(string orderId, OrderItem? item);
    }
}
