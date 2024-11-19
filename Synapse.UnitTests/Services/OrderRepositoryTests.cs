using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Synapse.Entities;
using Synapse.Interfaces;
using Synapse.Services;
using System.Net;

namespace Synapse.Tests.Services
{
    /** 
        Tests cover: 
        successful order retrieval
        error handling
        empty response handling
        order update with items
        **/
    public class OrderRepositoryTests
    {
        private readonly Mock<IHttpClientWrapper> _mockHttpClient;
        private readonly string _ordersApiUrl = "http://api/orders";
        private readonly string _updateApiUrl = "http://api/update";
        private readonly OrderRepository _repository;
        private readonly Mock<ILogger<OrderRepository>> _mockLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderRepositoryTests"/> class.
        /// </summary>
        public OrderRepositoryTests()
        {
            _mockHttpClient = new Mock<IHttpClientWrapper>();
            _mockLogger = new Mock<ILogger<OrderRepository>>();
            _repository = new OrderRepository(_mockHttpClient.Object, _ordersApiUrl, _updateApiUrl,
            _mockLogger.Object);
        }

        /// <summary>
        /// Tests that FetchOrders returns orders when the API call is successful.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task FetchOrders_WhenSuccessful_ReturnsOrders()
        {
            // Arrange
            Order[] orders = new[]
            {
                    new Order(
                        OrderId: "ORD-001",
                        Items:
                        [
                            new OrderItem("Item 1", "Pending", 1),
                            new OrderItem("Item 2", "Shipped", 2)
                        ]
                    ),
                    new Order(
                        OrderId: "ORD-002",
                        Items:
                        [
                            new OrderItem("Item 3", "Delivered", 3)
                        ]
                    )
                };

            string jsonResponse = JArray.FromObject(orders).ToString();
            HttpResponseMessage response = new()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            };

            _mockHttpClient
                .Setup(x => x.GetAsync(_ordersApiUrl))
                .ReturnsAsync(response);

            // Act
            IEnumerable<Order> result = await _repository.FetchOrders();

            // Assert
            Assert.NotNull(result);
            List<Order> ordersList = result.ToList();
            Assert.Equal(2, ordersList.Count);

            // Verify first order
            Order firstOrder = ordersList[0];
            Assert.Equal("ORD-001", firstOrder.OrderId);
            Assert.Equal(2, firstOrder.Items.Count);
            Assert.Equal("Item 1", firstOrder.Items[0].Description);
            Assert.Equal("Pending", firstOrder.Items[0].Status);
            Assert.Equal(1, firstOrder.Items[0].DeliveryNotification);

            // Verify second order
            Order secondOrder = ordersList[1];
            Assert.Equal("ORD-002", secondOrder.OrderId);
            Assert.Single(secondOrder.Items);
            Assert.Equal("Item 3", secondOrder.Items[0].Description);
            Assert.Equal("Delivered", secondOrder.Items[0].Status);
            Assert.Equal(3, secondOrder.Items[0].DeliveryNotification);

            _mockHttpClient.Verify(x => x.GetAsync(_ordersApiUrl), Times.Once);
        }

        /// <summary>
        /// Tests that FetchOrders returns an empty collection when the API call fails.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task FetchOrders_WhenApiFailure_ReturnsEmptyCollection()
        {
            // Arrange
            HttpResponseMessage response = new()
            {
                StatusCode = HttpStatusCode.InternalServerError
            };

            _mockHttpClient
                .Setup(x => x.GetAsync(_ordersApiUrl))
                .ReturnsAsync(response);

            // Act
            IEnumerable<Order> result = await _repository.FetchOrders();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockHttpClient.Verify(x => x.GetAsync(_ordersApiUrl), Times.Once);
        }

        /// <summary>
        /// Tests that UpdateOrder sends the correct request to the API.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task UpdateOrder_SendsCorrectRequest()
        {
            // Arrange
            Order order = new(
                OrderId: "ORD-001",
                Items:
                [
                    new OrderItem("Test Item", "Processing", 1)
                ]
            );

            HttpResponseMessage response = new()
            {
                StatusCode = HttpStatusCode.OK
            };

            _mockHttpClient
                .Setup(x => x.PostAsync(
                    _updateApiUrl,
                    It.IsAny<StringContent>()))
                .ReturnsAsync(response);

            // Act
            await _repository.UpdateOrder(order);

            // Assert
            _mockHttpClient.Verify(x => x.PostAsync(
                _updateApiUrl,
                It.Is<StringContent>(content =>
                    VerifyJsonContent(content, order))),
                Times.Once);
        }

        /// <summary>
        /// Tests that FetchOrders returns an empty collection when the API response is empty.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task FetchOrders_WithEmptyResponse_ReturnsEmptyCollection()
        {
            // Arrange
            HttpResponseMessage response = new()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            };

            _mockHttpClient
                .Setup(x => x.GetAsync(_ordersApiUrl))
                .ReturnsAsync(response);

            // Act
            IEnumerable<Order> result = await _repository.FetchOrders();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockHttpClient.Verify(x => x.GetAsync(_ordersApiUrl), Times.Once);
        }

        /// <summary>
        /// Helps verify the JSON content sent in update requests.
        /// </summary>
        /// <param name="content">The JSON content sent in the request.</param>
        /// <param name="expectedOrder">The expected order object.</param>
        /// <returns>True if the content matches the expected order; otherwise, false.</returns>
        private bool VerifyJsonContent(StringContent content, Order expectedOrder)
        {
            string contentString = content.ReadAsStringAsync().Result;
            JObject jsonObject = JObject.Parse(contentString);
            Order actualOrder = new(
                OrderId: jsonObject["OrderId"].ToString(),
                Items: jsonObject["Items"].ToObject<List<OrderItem>>()
            );

            if (actualOrder.OrderId != expectedOrder.OrderId)
                return false;

            if (actualOrder.Items.Count != expectedOrder.Items.Count)
                return false;

            for (int i = 0; i < actualOrder.Items.Count; i++)
            {
                OrderItem actualItem = actualOrder.Items[i];
                OrderItem expectedItem = expectedOrder.Items[i];

                if (actualItem.Description != expectedItem.Description ||
                    actualItem.Status != expectedItem.Status ||
                    actualItem.DeliveryNotification != expectedItem.DeliveryNotification)
                {
                    return false;
                }
            }

            return true;
        }
    }
}