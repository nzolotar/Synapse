namespace Synapse.Interfaces
{
    /// <summary>
    /// Defines the contract for processing orders.
    /// </summary>
    public interface IOrderProcessor
    {
        /// <summary>
        /// Processes the orders asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ProcessOrders();
    }
}
