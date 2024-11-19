using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Synapse.Entities;
using Synapse.Interfaces;
using Synapse.Services;

namespace Synapse.Tests.Services
{
    /*
    tests cover: 
    Constructor validation
    Basic order processing
    Delivery notification handling
    Error handling
    Status validation
    */
    public class OrderProcessorTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IOrderRepository> _orderRepositoryMock;
        private readonly Mock<IAlertService> _alertServiceMock;
        private readonly Mock<ILogger<OrderProcessor>> _loggerMock;
        private readonly OrderProcessor _sut;

        public OrderProcessorTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _orderRepositoryMock = _fixture.Freeze<Mock<IOrderRepository>>();
            _alertServiceMock = _fixture.Freeze<Mock<IAlertService>>();
            _loggerMock = _fixture.Freeze<Mock<ILogger<OrderProcessor>>>();
            _sut = new OrderProcessor(
                _orderRepositoryMock.Object,
                _alertServiceMock.Object,
                _loggerMock.Object
            );
        }

        /// <summary>
        /// Constructor Validation test: null checks for all dependencies
        /// </summary>
        [Fact]
        public void Constructor_WithNullDependencies_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OrderProcessor(null, _alertServiceMock.Object, _loggerMock.Object))
                .ParamName.Should().Be("orderRepository");

            Assert.Throws<ArgumentNullException>(() => new OrderProcessor(_orderRepositoryMock.Object, null, _loggerMock.Object))
                .ParamName.Should().Be("alertService");

            Assert.Throws<ArgumentNullException>(() => new OrderProcessor(_orderRepositoryMock.Object, _alertServiceMock.Object, null))
                .ParamName.Should().Be("logger");
        }

        #region "order processing tests"
        /// <summary>
        /// Basic Order Processing: empty order list, ensures no alerts or logging occurs
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProcessOrders_WithNoOrders_DoesNothing()
        {
            // Arrange
            _orderRepositoryMock.Setup(x => x.FetchOrders())
                .ReturnsAsync(Array.Empty<Order>());

            // Act
            await _sut.ProcessOrders();

            // Assert
            _alertServiceMock.Verify(x => x.SendAlert(It.IsAny<string>(), It.IsAny<OrderItem>()), Times.Never);
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcessOrders_WithDeliveredItems_ProcessesCorrectly()
        {
            // Arrange
            Order[] orders = new[]
            {
                new Order("ORDER-1",
                [
                    new OrderItem("Item1", "Delivered", 0),
                    new OrderItem("Item2", "InProgress", 0)
                ])
            };

            _orderRepositoryMock.Setup(x => x.FetchOrders())
                .ReturnsAsync(orders);

            // Act
            await _sut.ProcessOrders();

            // Assert
            _alertServiceMock.Verify(
                x => x.SendAlert("ORDER-1", It.Is<OrderItem>(i => i.Description == "Item1")),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("ORDER-1") && v.ToString().Contains("Item1")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessOrders_WithMultipleOrders_ProcessesAllOrders()
        {
            // Arrange
            Order[] orders = new[]
            {
                new Order("ORDER-1", [new OrderItem("Item1", "Delivered", 0)]),
                new Order("ORDER-2", [new OrderItem("Item2", "Delivered", 0)])
            };

            _orderRepositoryMock.Setup(x => x.FetchOrders())
                .ReturnsAsync(orders);

            // Act
            await _sut.ProcessOrders();

            // Assert
            _alertServiceMock.Verify(
                x => x.SendAlert(It.IsAny<string>(), It.IsAny<OrderItem>()),
                Times.Exactly(2));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Exactly(2));
        }
        #endregion

        #region "Delivery notification handling"
        [Fact]
        public async Task ProcessOrders_UpdatesDeliveryNotificationsCorrectly()
        {
            // Arrange
            Order order = new("ORDER-1",
            [
                new OrderItem("Item1", "Delivered", 1),
                new OrderItem("Item2", "InProgress", 0)
            ]);

            _orderRepositoryMock.Setup(x => x.FetchOrders())
                .ReturnsAsync(new[] { order });

            Order? capturedOrder = null;
            _orderRepositoryMock.Setup(x => x.UpdateOrder(It.IsAny<Order>()))
                .Callback<Order>(o => capturedOrder = o);

            // Act
            await _sut.ProcessOrders();

            // Assert
            _alertServiceMock.Verify(
                x => x.SendAlert("ORDER-1",
                    It.Is<OrderItem>(i => i.Description == "Item1" && i.DeliveryNotification == 1)),
                Times.Once);

            capturedOrder.Should().NotBeNull();
            capturedOrder!.Items.Should().HaveCount(2);
            capturedOrder.Items.Should().Contain(i =>
                i.Description == "Item1" &&
                i.Status == "Delivered" &&
                i.DeliveryNotification == 2);
            capturedOrder.Items.Should().Contain(i =>
                i.Description == "Item2" &&
                i.Status == "InProgress" &&
                i.DeliveryNotification == 0);
        }

        #endregion

        #region "Status Validation"
        /// <summary>
        /// Status Validation: Tests case-insensitive status matching, validates different variations of "Delivered"
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        [Theory]
        [InlineData("Delivered")]
        [InlineData("DELIVERED")]
        [InlineData("delivered")]
        public async Task ProcessOrders_WithDifferentDeliveredCasing_ProcessesCorrectly(string status)
        {
            // Arrange
            Order order = new("ORDER-1",
            [
                new OrderItem("Item1", status, 0)
            ]);

            _orderRepositoryMock.Setup(x => x.FetchOrders())
                .ReturnsAsync(new[] { order });

            // Act
            await _sut.ProcessOrders();

            // Assert
            _alertServiceMock.Verify(
                x => x.SendAlert("ORDER-1", It.Is<OrderItem>(i => i.Description == "Item1")),
                Times.Once);
        }
        #endregion

        #region "Error Handling"
        [Fact]
        public async Task ProcessOrders_WhenAlertFails_SkipsItemAndLogs()
        {
            // Arrange
            Order order = new("ORDER-1",
            [
                new OrderItem("Item1", "Delivered", 0),
                new OrderItem("Item2", "Delivered", 0)
            ]);

            _orderRepositoryMock.Setup(x => x.FetchOrders())
                .ReturnsAsync(new[] { order });

            _alertServiceMock
                .Setup(x => x.SendAlert("ORDER-1", It.Is<OrderItem>(i => i.Description == "Item1")))
                .ThrowsAsync(new Exception("Alert failed"));

            // Act
            await _sut.ProcessOrders();

            // Assert
            _alertServiceMock.Verify(
                x => x.SendAlert("ORDER-1", It.Is<OrderItem>(i => i.Description == "Item2")),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("ORDER-1") && v.ToString().Contains("Item1")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        [Fact]
        public async Task ProcessOrders_WhenFetchFails_ThrowsException()
        {
            // Arrange
            _orderRepositoryMock.Setup(x => x.FetchOrders())
                .ThrowsAsync(new Exception("Fetch failed"));

            // Act
            Func<Task> act = _sut.ProcessOrders;

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Fetch failed");
        }
        #endregion
    }
}