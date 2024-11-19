using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Synapse.Entities;
using Synapse.Interfaces;
using Synapse.Services;
using System.Net;
/**
AlertServiceTests cover these key scenarios:

Successful alert sending and content verification
Failed request handling
Input validation (null/empty orderId, null orderItem)
Constructor validation (invalid URL, null HTTP client)

**/


namespace Synapse.Tests.Services
{
    /// <summary>
    /// Unit tests for the AlertService class.
    /// </summary>
    public class AlertServiceTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IHttpClientWrapper> _httpClientMock;
        private readonly string _alertApiUrl;
        private readonly AlertService _sut;
        private readonly Mock<ILogger<AlertService>> _loggerMock;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlertServiceTests"/> class.
        /// </summary>
        public AlertServiceTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _httpClientMock = _fixture.Freeze<Mock<IHttpClientWrapper>>();
            _alertApiUrl = "https://alert-api.com/alerts";
            _loggerMock = _fixture.Freeze<Mock<ILogger<AlertService>>>();
            _sut = new AlertService(_httpClientMock.Object, _alertApiUrl, _loggerMock.Object);
        }

        /// <summary>
        /// Tests that SendAlert successfully sends an alert and verifies the content.
        /// </summary>
        [Fact]
        public async Task SendAlert_SuccessfulRequest_ReturnsSuccessfully()
        {
            // Arrange
            string orderId = "ORD-123";
            OrderItem orderItem = new("Test Item", "Delivered", 1);

            _httpClientMock
                .Setup(x => x.PostAsync(
                    It.IsAny<string>(),
                    It.IsAny<StringContent>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            await _sut.SendAlert(orderId, orderItem);

            // Assert
            _httpClientMock.Verify(
                x => x.PostAsync(
                    _alertApiUrl,
                    It.Is<StringContent>(content => VerifyAlertContent(content, orderId, orderItem))),
                Times.Once);
        }

        /// <summary>
        /// Tests that SendAlert throws an exception when the request fails.
        /// </summary>
        [Fact]
        public async Task SendAlert_FailedRequest_ThrowsException()
        {
            // Arrange
            string orderId = "ORD-123";
            OrderItem orderItem = new("Test Item", "Delivered", 1);

            _httpClientMock
                .Setup(x => x.PostAsync(
                    It.IsAny<string>(),
                    It.IsAny<StringContent>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            // Act
            Func<Task> act = async () => await _sut.SendAlert(orderId, orderItem);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>()
                .WithMessage($"Failed to send alert for order {orderId}");
        }

        /// <summary>
        /// Tests that SendAlert throws an ArgumentException for invalid orderId.
        /// </summary>
        /// <param name="invalidOrderId">The invalid orderId to test.</param>
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task SendAlert_InvalidOrderId_ThrowsArgumentException(string invalidOrderId)
        {
            // Arrange
            OrderItem orderItem = new("Test Item", "Delivered", 1);

            // Act
            Func<Task> act = async () => await _sut.SendAlert(invalidOrderId, orderItem);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("OrderId cannot be null or empty");
        }

        /// <summary>
        /// Tests that SendAlert throws an ArgumentNullException for null orderItem.
        /// </summary>
        [Fact]
        public async Task SendAlert_NullOrderItem_ThrowsArgumentNullException()
        {
            // Arrange
            string orderId = "ORD-123";
            OrderItem? orderItem = null;

            // Act
            Func<Task> act = async () => await _sut.SendAlert(orderId, orderItem);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("item");
        }

        /// <summary>
        /// Tests that the constructor throws an ArgumentException for invalid API URL.
        /// </summary>
        /// <param name="invalidUrl">The invalid API URL to test.</param>
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Constructor_InvalidApiUrl_ThrowsArgumentException(string invalidUrl)
        {
            // Act
            Action act = () => new AlertService(_httpClientMock.Object, invalidUrl, _loggerMock.Object);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Alert API URL can not be blank");
        }

        /// <summary>
        /// Tests that the constructor throws an ArgumentNullException for null HTTP client.
        /// </summary>
        [Fact]
        public void Constructor_NullHttpClient_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new AlertService(null, _alertApiUrl, _loggerMock.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("httpClient");
        }

        /// <summary>
        /// Verifies the content of the alert message.
        /// </summary>
        /// <param name="content">The content to verify.</param>
        /// <param name="orderId">The order ID.</param>
        /// <param name="item">The order item.</param>
        /// <returns>True if the content is valid; otherwise, false.</returns>
        private bool VerifyAlertContent(StringContent content, string orderId, OrderItem item)
        {
            string contentString = content.ReadAsStringAsync().GetAwaiter().GetResult();
            JObject alertData = JObject.Parse(contentString);

            string expectedMessage = $"Alert for delivered item: Order {orderId}, Item: {item.Description}, " +
                                $"Delivery Notifications: {item.DeliveryNotification}";

            return alertData["Message"]?.ToString() == expectedMessage &&
                   content.Headers.ContentType?.MediaType == "application/json";
        }
    }

}