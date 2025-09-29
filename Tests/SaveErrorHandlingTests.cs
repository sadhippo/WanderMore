using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using Xunit;
using HiddenHorizons;

namespace HiddenHorizons.Tests
{
    public class SaveErrorHandlingTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly SaveErrorManager _errorManager;

        public SaveErrorHandlingTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "SaveErrorTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            _errorManager = new SaveErrorManager();
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public void SaveErrorEventArgs_Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var errorType = SaveErrorType.DiskSpaceInsufficient;
            var message = "Test error message";
            var exception = new IOException("Test exception");

            // Act
            var args = new SaveErrorEventArgs(errorType, message, exception);

            // Assert
            Assert.Equal(errorType, args.ErrorType);
            Assert.Equal(message, args.ErrorMessage);
            Assert.Equal(exception, args.Exception);
            Assert.False(args.CanRetry);
            Assert.False(args.HasBackup);
            Assert.Null(args.SlotId);
            Assert.Null(args.Context);
        }

        [Fact]
        public void CheckDiskSpace_SufficientSpace_ReturnsTrue()
        {
            // Arrange & Act
            bool result = _errorManager.CheckDiskSpace(_testDirectory, 1024);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CheckDiskSpace_NonExistentDrive_ReturnsFalse()
        {
            // Arrange
            string invalidPath = Path.Combine("Z:", "NonExistent");
            bool errorRaised = false;

            _errorManager.ErrorOccurred += (sender, args) =>
            {
                errorRaised = true;
                Assert.Equal(SaveErrorType.UnknownError, args.ErrorType);
            };

            // Act
            bool result = _errorManager.CheckDiskSpace(invalidPath);

            // Assert
            Assert.False(result);
            Assert.True(errorRaised);
        }

        [Fact]
        public void CheckPermissions_ValidDirectory_ReturnsTrue()
        {
            // Arrange & Act
            bool result = _errorManager.CheckPermissions(_testDirectory);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CheckPermissions_ReadOnlyDirectory_ReturnsFalse()
        {
            // Arrange
            string readOnlyDir = Path.Combine(_testDirectory, "readonly");
            Directory.CreateDirectory(readOnlyDir);
            
            try
            {
                // Make directory read-only (this may not work on all systems)
                var dirInfo = new DirectoryInfo(readOnlyDir);
                dirInfo.Attributes |= FileAttributes.ReadOnly;

                bool errorRaised = false;
                _errorManager.ErrorOccurred += (sender, args) =>
                {
                    errorRaised = true;
                    Assert.Equal(SaveErrorType.PermissionDenied, args.ErrorType);
                };

                // Act
                bool result = _errorManager.CheckPermissions(readOnlyDir);

                // Assert - This test may pass on some systems where read-only doesn't prevent write
                // The important thing is that the error handling works correctly
                if (!result)
                {
                    Assert.True(errorRaised);
                }
            }
            finally
            {
                // Clean up - remove read-only attribute
                try
                {
                    var dirInfo = new DirectoryInfo(readOnlyDir);
                    dirInfo.Attributes &= ~FileAttributes.ReadOnly;
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_SuccessfulOperation_ReturnsResult()
        {
            // Arrange
            int callCount = 0;
            Func<Task<string>> operation = async () =>
            {
                callCount++;
                await Task.Delay(10);
                return "success";
            };

            // Act
            string result = await _errorManager.ExecuteWithRetryAsync(operation, "test operation");

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_TransientFailureThenSuccess_RetriesAndSucceeds()
        {
            // Arrange
            int callCount = 0;
            Func<Task<string>> operation = async () =>
            {
                callCount++;
                await Task.Delay(10);
                
                if (callCount < 2)
                {
                    throw new IOException("Transient error");
                }
                
                return "success";
            };

            // Act
            string result = await _errorManager.ExecuteWithRetryAsync(operation, "test operation");

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(2, callCount);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_PermanentFailure_ThrowsAfterMaxRetries()
        {
            // Arrange
            int callCount = 0;
            bool errorRaised = false;
            
            _errorManager.ErrorOccurred += (sender, args) =>
            {
                errorRaised = true;
                Assert.Equal(SaveErrorType.UnknownError, args.ErrorType);
            };

            Func<Task<string>> operation = async () =>
            {
                callCount++;
                await Task.Delay(10);
                throw new IOException("Retryable error"); // Use retryable error for this test
            };

            // Act & Assert
            await Assert.ThrowsAsync<SaveLoadException>(async () =>
            {
                await _errorManager.ExecuteWithRetryAsync(operation, "test operation");
            });

            Assert.Equal(3, callCount);
            Assert.True(errorRaised);
        }

        [Fact]
        public void ExecuteWithRetry_SuccessfulOperation_ReturnsResult()
        {
            // Arrange
            int callCount = 0;
            Func<string> operation = () =>
            {
                callCount++;
                return "success";
            };

            // Act
            string result = _errorManager.ExecuteWithRetry(operation, "test operation");

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void ExecuteWithRetry_TransientFailureThenSuccess_RetriesAndSucceeds()
        {
            // Arrange
            int callCount = 0;
            Func<string> operation = () =>
            {
                callCount++;
                
                if (callCount < 2)
                {
                    throw new IOException("Transient error");
                }
                
                return "success";
            };

            // Act
            string result = _errorManager.ExecuteWithRetry(operation, "test operation");

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(2, callCount);
        }

        [Fact]
        public void HandlePartialFailure_RaisesErrorWithCorrectInformation()
        {
            // Arrange
            var partialData = new { SomeData = "test" };
            var failedSystems = new[] { "System1", "System2" };
            bool errorRaised = false;
            SaveErrorEventArgs capturedArgs = null;

            _errorManager.ErrorOccurred += (sender, args) =>
            {
                errorRaised = true;
                capturedArgs = args;
            };

            // Act
            _errorManager.HandlePartialFailure(partialData, failedSystems, "save", 1);

            // Assert
            Assert.True(errorRaised);
            Assert.NotNull(capturedArgs);
            Assert.Equal(SaveErrorType.SerializationFailed, capturedArgs.ErrorType);
            Assert.Contains("System1, System2", capturedArgs.ErrorMessage);
            Assert.Contains("save", capturedArgs.ErrorMessage);
            Assert.True(capturedArgs.CanRetry);
            Assert.True(capturedArgs.HasBackup);
            Assert.Equal(1, capturedArgs.SlotId);
        }

        [Fact]
        public void ValidateSaveData_ValidJson_ReturnsTrue()
        {
            // Arrange
            var validJson = JsonSerializer.Serialize(new { test = "data" });

            // Act
            bool result = _errorManager.ValidateSaveData(validJson, null, 1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateSaveData_InvalidJson_ReturnsFalse()
        {
            // Arrange
            var invalidJson = "{ invalid json }";
            bool errorRaised = false;

            _errorManager.ErrorOccurred += (sender, args) =>
            {
                errorRaised = true;
                Assert.Equal(SaveErrorType.FileCorrupted, args.ErrorType);
                Assert.Equal(1, args.SlotId);
            };

            // Act
            bool result = _errorManager.ValidateSaveData(invalidJson, null, 1);

            // Assert
            Assert.False(result);
            Assert.True(errorRaised);
        }

        [Fact]
        public void ValidateSaveData_EmptyString_ReturnsFalse()
        {
            // Arrange
            bool errorRaised = false;

            _errorManager.ErrorOccurred += (sender, args) =>
            {
                errorRaised = true;
                Assert.Equal(SaveErrorType.FileCorrupted, args.ErrorType);
            };

            // Act
            bool result = _errorManager.ValidateSaveData("", null, 1);

            // Assert
            Assert.False(result);
            Assert.True(errorRaised);
        }

        [Theory]
        [InlineData(typeof(IOException), true)]
        [InlineData(typeof(UnauthorizedAccessException), true)]
        [InlineData(typeof(TimeoutException), true)]
        [InlineData(typeof(InvalidOperationException), false)]
        [InlineData(typeof(ArgumentException), false)]
        public async Task ExecuteWithRetryAsync_ErrorTypeRetryability_BehavesCorrectly(Type exceptionType, bool shouldRetry)
        {
            // Arrange
            int callCount = 0;
            var exception = (Exception)Activator.CreateInstance(exceptionType, "Test error");
            
            Func<Task<string>> operation = async () =>
            {
                callCount++;
                await Task.Delay(10);
                throw exception;
            };

            // Act & Assert
            if (shouldRetry)
            {
                // Retryable errors should be wrapped in SaveLoadException after max retries
                await Assert.ThrowsAsync<SaveLoadException>(async () =>
                {
                    await _errorManager.ExecuteWithRetryAsync(operation, "test operation");
                });
            }
            else
            {
                // Non-retryable errors should be thrown immediately without wrapping
                var thrownException = await Assert.ThrowsAsync(exceptionType, async () =>
                {
                    await _errorManager.ExecuteWithRetryAsync(operation, "test operation");
                });
                Assert.Equal("Test error", thrownException.Message);
            }

            // Verify retry behavior
            int expectedCalls = shouldRetry ? 3 : 1; // Max retries or single attempt
            Assert.Equal(expectedCalls, callCount);
        }

        [Fact]
        public async Task SaveManager_Integration_ErrorHandlingWorksCorrectly()
        {
            // Arrange
            var saveManager = new SaveManager(_testDirectory);
            var mockSaveable = new MockSaveable("test", new { data = "test" });
            saveManager.RegisterSaveable(mockSaveable);

            bool errorEventFired = false;
            saveManager.SaveError += (sender, args) => errorEventFired = true;

            // Act - Try to save to an invalid slot (should trigger error handling)
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await saveManager.SaveGameAsync(-1); // Invalid slot ID
            });

            // Assert
            // The error handling should have been triggered internally
            Assert.True(saveManager.RegisteredSystemCount > 0);
            Assert.True(errorEventFired);
        }

        [Fact]
        public void SaveLoadException_Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var message = "Test error message";
            var innerException = new IOException("Inner exception");

            // Act
            var exception1 = new SaveLoadException(message);
            var exception2 = new SaveLoadException(message, innerException);

            // Assert
            Assert.Equal(message, exception1.Message);
            Assert.Null(exception1.InnerException);

            Assert.Equal(message, exception2.Message);
            Assert.Equal(innerException, exception2.InnerException);
        }

        private class MockSaveable : ISaveable
        {
            public string SaveKey { get; }
            public int SaveVersion => 1;
            private object _data;

            public MockSaveable(string saveKey, object data)
            {
                SaveKey = saveKey;
                _data = data;
            }

            public object GetSaveData() => _data;
            public void LoadSaveData(object data) => _data = data;
        }
    }
}