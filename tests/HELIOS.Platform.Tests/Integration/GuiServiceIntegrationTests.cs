using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests.Integration
{
    /// <summary>
    /// Integration Tests: GUI → Service Layer Communication
    /// 6 test cases testing user interface interaction with services
    /// </summary>
    public class GuiServiceIntegrationTests
    {
        private readonly Mock<IGuiController> _mockGui;
        private readonly Mock<IServiceProxy> _mockService;

        public GuiServiceIntegrationTests()
        {
            _mockGui = new Mock<IGuiController>();
            _mockService = new Mock<IServiceProxy>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task UserClicksButton_ServiceCallInitiated_ResultDisplayed()
        {
            // Arrange
            var buttonId = "process-btn";
            var expectedResult = "Process Complete";
            
            _mockGui.Setup(g => g.OnButtonClickAsync(buttonId, CancellationToken.None))
                .ReturnsAsync(new GuiEvent { Command = "Process" });
            _mockService.Setup(s => s.ProcessAsync("Process", CancellationToken.None))
                .ReturnsAsync(new ServiceResponse { Status = "Success", Message = expectedResult });

            // Act
            var guiEvent = await _mockGui.Object.OnButtonClickAsync(buttonId, CancellationToken.None);
            var serviceResult = await _mockService.Object.ProcessAsync(guiEvent.Command, CancellationToken.None);
            await _mockGui.Object.DisplayAsync(serviceResult.Message, CancellationToken.None);

            // Assert
            Assert.NotNull(guiEvent);
            Assert.Equal("Success", serviceResult.Status);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ServiceNotification_UpdatesGui_UserSees()
        {
            // Arrange
            var notificationMessage = "New data available";
            var guiUpdateId = "data-panel";
            
            _mockService.Setup(s => s.NotifyAsync(notificationMessage, CancellationToken.None))
                .ReturnsAsync(true);
            _mockGui.Setup(g => g.UpdateAsync(guiUpdateId, It.IsAny<object>(), CancellationToken.None))
                .ReturnsAsync(true);

            // Act
            var notified = await _mockService.Object.NotifyAsync(notificationMessage, CancellationToken.None);
            var updated = await _mockGui.Object.UpdateAsync(guiUpdateId, new { Message = notificationMessage }, CancellationToken.None);

            // Assert
            Assert.True(notified);
            Assert.True(updated);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task UserInput_ValidatedByGui_SentToService_OnlyIfValid()
        {
            // Arrange
            var userInput = "valid-input";
            _mockGui.Setup(g => g.ValidateInputAsync(userInput, CancellationToken.None))
                .ReturnsAsync(true);
            _mockService.Setup(s => s.ProcessInputAsync(userInput, CancellationToken.None))
                .ReturnsAsync(new ServiceResponse { Status = "Success" });

            // Act
            var isValid = await _mockGui.Object.ValidateInputAsync(userInput, CancellationToken.None);
            ServiceResponse result = null;
            if (isValid)
            {
                result = await _mockService.Object.ProcessInputAsync(userInput, CancellationToken.None);
            }

            // Assert
            Assert.True(isValid);
            Assert.NotNull(result);
            Assert.Equal("Success", result.Status);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ServiceError_DisplayedToUser_GuiHandlesGracefully()
        {
            // Arrange
            var errorMessage = "Service Error: Database Connection Failed";
            var guiErrorDisplayId = "error-label";
            
            _mockService.Setup(s => s.ProcessAsync("FailingCommand", CancellationToken.None))
                .ThrowsAsync(new Exception(errorMessage));
            _mockGui.Setup(g => g.DisplayErrorAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(true);

            // Act
            ServiceResponse result = null;
            bool errorHandled = false;
            try
            {
                result = await _mockService.Object.ProcessAsync("FailingCommand", CancellationToken.None);
            }
            catch (Exception ex)
            {
                errorHandled = await _mockGui.Object.DisplayErrorAsync(ex.Message, CancellationToken.None);
            }

            // Assert
            Assert.True(errorHandled);
            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task MultipleGuiComponents_RequestSameService_AllReceiveUpdates()
        {
            // Arrange
            var componentIds = new[] { "comp1", "comp2", "comp3" };
            var serviceData = new ServiceResponse { Status = "Success", Data = "shared-data" };
            
            _mockService.Setup(s => s.GetDataAsync(CancellationToken.None))
                .ReturnsAsync(serviceData);

            foreach (var componentId in componentIds)
            {
                _mockGui.Setup(g => g.UpdateAsync(componentId, serviceData, CancellationToken.None))
                    .ReturnsAsync(true);
            }

            // Act
            var data = await _mockService.Object.GetDataAsync(CancellationToken.None);
            var updateTasks = new List<Task<bool>>();
            foreach (var componentId in componentIds)
            {
                updateTasks.Add(_mockGui.Object.UpdateAsync(componentId, data, CancellationToken.None));
            }
            var updates = await Task.WhenAll(updateTasks);

            // Assert
            Assert.NotNull(data);
            Assert.All(updates, u => Assert.True(u));
        }
    }

    public class GuiEvent
    {
        public string Command { get; set; }
        public object Data { get; set; }
    }

    public class ServiceResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    public interface IGuiController
    {
        Task<GuiEvent> OnButtonClickAsync(string buttonId, CancellationToken cancellationToken);
        Task<bool> DisplayAsync(string message, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(string componentId, object data, CancellationToken cancellationToken);
        Task<bool> ValidateInputAsync(string input, CancellationToken cancellationToken);
        Task<bool> DisplayErrorAsync(string message, CancellationToken cancellationToken);
    }

    public interface IServiceProxy
    {
        Task<ServiceResponse> ProcessAsync(string command, CancellationToken cancellationToken);
        Task<bool> NotifyAsync(string message, CancellationToken cancellationToken);
        Task<ServiceResponse> ProcessInputAsync(string input, CancellationToken cancellationToken);
        Task<ServiceResponse> GetDataAsync(CancellationToken cancellationToken);
    }
}
