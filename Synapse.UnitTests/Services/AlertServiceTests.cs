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
    public class AlertServiceTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IHttpClientWrapper> _httpClientMock;
        private readonly string _alertApiUrl;
        private readonly AlertService _sut;
        private readonly Mock<ILogger<AlertService>> _loggerMock;

        public AlertServiceTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _httpClientMock = _fixture.Freeze<Mock<IHttpClientWrapper>>();
            _alertApiUrl = "https://alert-api.com/alerts";
            _loggerMock = _fixture.Freeze<Mock<ILogger<AlertService>>>();
            _sut = new AlertService(_httpClientMock.Object, _alertApiUrl, _loggerMock.Object);
        }

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

        [Fact]
        public void Constructor_NullHttpClient_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new AlertService(null, _alertApiUrl, _loggerMock.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("httpClient");
        }

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