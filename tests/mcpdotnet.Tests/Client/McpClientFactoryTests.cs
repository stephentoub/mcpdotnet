using System.Threading.Channels;
using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace McpDotNet.Tests.Client;

public class McpClientFactoryTests
{
    private readonly McpClientOptions _defaultOptions = new()
    {
        ClientInfo = new() { Name = "TestClient", Version = "1.0.0" }
    };

    [Fact]
    public async Task GetClientAsync_WithValidStdioConfig_CreatesNewClient()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = TransportTypes.StdIo,
            Location = "/path/to/server",
            TransportOptions = new Dictionary<string, string>
            {
                ["arguments"] = "--test arg",
                ["workingDirectory"] = "/working/dir"
            }
        };

        // Create a mock transport
        var mockTransport = new Mock<IClientTransport>();
        mockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(t => t.IsConnected).Returns(true);
        mockTransport.Setup(t => t.MessageReader).Returns(Mock.Of<ChannelReader<IJsonRpcMessage>>());

        // Create a mock client
        var mockClient = new Mock<IMcpClient>();
        mockClient.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockClient.Setup(c => c.IsInitialized).Returns(true);

        // Inject the mock transport into the factory
        using var factory = new McpClientFactory(
            [config],
            _defaultOptions,
            NullLoggerFactory.Instance,
            transportFactoryMethod: _ => mockTransport.Object,
            clientFactoryMethod: (_, _, _) => mockClient.Object
        );

        // Act
        var client = await factory.GetClientAsync("test-server");

        // Assert
        Assert.NotNull(client);
        // We could add more assertions here about the client's configuration
    }

    [Fact]
    public async Task GetClientAsync_CalledTwice_ReturnsSameInstance()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = TransportTypes.StdIo,
            Location = "/path/to/server"
        };

        // Create a mock transport
        var mockTransport = new Mock<IClientTransport>();
        mockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(t => t.IsConnected).Returns(true);
        mockTransport.Setup(t => t.MessageReader).Returns(Mock.Of<ChannelReader<IJsonRpcMessage>>());

        // Create a mock client
        var mockClient = new Mock<IMcpClient>();
        mockClient.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockClient.Setup(c => c.IsInitialized).Returns(true);

        using var factory = new McpClientFactory([config],
            _defaultOptions,
            NullLoggerFactory.Instance,
            transportFactoryMethod: _ => mockTransport.Object,
            clientFactoryMethod: (_, _, _) => mockClient.Object);

        // Act
        var client1 = await factory.GetClientAsync("test-server");
        var client2 = await factory.GetClientAsync("test-server");

        // Assert
        Assert.Same(client1, client2);
    }

    [Fact]
    public async Task GetClientAsync_WithInvalidServerId_ThrowsArgumentException()
    {
        // Arrange
        using var factory = new McpClientFactory([],
            _defaultOptions,
            NullLoggerFactory.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => factory.GetClientAsync("non-existent-server")
        );
    }

    [Fact]
    public async Task GetClientAsync_WithUnsupportedTransport_ThrowsArgumentException()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = "unsupported",
            Location = "/path/to/server"
        };

        using var factory = new McpClientFactory([config], _defaultOptions,
            NullLoggerFactory.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => factory.GetClientAsync("test-server")
        );
    }

    [Fact]
    public async Task GetClientAsync_WithNoTransportOptions_CreatesClientWithDefaults()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = TransportTypes.StdIo,
            Location = "/path/to/server"
        };

        // Create a mock transport
        var mockTransport = new Mock<IClientTransport>();
        mockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(t => t.IsConnected).Returns(true);
        mockTransport.Setup(t => t.MessageReader).Returns(Mock.Of<ChannelReader<IJsonRpcMessage>>());

        // Create a mock client
        var mockClient = new Mock<IMcpClient>();
        mockClient.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockClient.Setup(c => c.IsInitialized).Returns(true);

        using var factory = new McpClientFactory([config],
            _defaultOptions,
            NullLoggerFactory.Instance,
            transportFactoryMethod: _ => mockTransport.Object,
            clientFactoryMethod: (_, _, _) => mockClient.Object);

        // Act
        var client = await factory.GetClientAsync("test-server");

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithDuplicateServerIds_ThrowsArgumentException()
    {
        // Arrange
        McpServerConfig[] configs =
        [
            new McpServerConfig { Id = "duplicate", Name = "duplicate", TransportType = TransportTypes.StdIo, Location = "/path1" },
            new McpServerConfig { Id = "duplicate", Name = "duplicate", TransportType = TransportTypes.StdIo, Location = "/path2" }
        ];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new McpClientFactory(configs, _defaultOptions,
            NullLoggerFactory.Instance));
    }

    [Fact]
    public async Task GetClientAsync_WithSseTransport_CanCreateClient()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = TransportTypes.Sse,
            Location = "http://localhost:8080"
        };

        // Create a mock transport
        var mockTransport = new Mock<IClientTransport>();
        mockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(t => t.IsConnected).Returns(true);
        mockTransport.Setup(t => t.MessageReader).Returns(Mock.Of<ChannelReader<IJsonRpcMessage>>());

        // Create a mock client
        var mockClient = new Mock<IMcpClient>();
        mockClient.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockClient.Setup(c => c.IsInitialized).Returns(true);

        // Inject the mock transport into the factory
        using var factory = new McpClientFactory(
            [config],
            _defaultOptions,
            NullLoggerFactory.Instance,
            transportFactoryMethod: _ => mockTransport.Object,
            clientFactoryMethod: (_, _, _) => mockClient.Object
        );

        // Act
        var client = await factory.GetClientAsync("test-server");

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void McpFactory_WithSse_CreatesCorrectTransportOptions()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = TransportTypes.Sse,
            Location = "http://localhost:8080",
            TransportOptions = new Dictionary<string, string>
            {
                ["connectionTimeout"] = "10",
                ["maxReconnectAttempts"] = "2",
                ["reconnectDelay"] = "5",
                ["header.test"] = "the_header_value"
            }
        };

        // Act
        using var factory = new McpClientFactory(
            [config],
            _defaultOptions,
            NullLoggerFactory.Instance
        );

        var transport = factory.TransportFactoryMethod(config) as SseClientTransport;

        // Assert
        Assert.NotNull(transport);
        Assert.Equal(TimeSpan.FromSeconds(10), transport.Options.ConnectionTimeout);
        Assert.Equal(2, transport.Options.MaxReconnectAttempts);
        Assert.Equal(TimeSpan.FromSeconds(5), transport.Options.ReconnectDelay);
        Assert.NotNull(transport.Options.AdditionalHeaders);
        Assert.Equal("the_header_value", transport.Options.AdditionalHeaders["test"]);
    }

    [Fact]
    public void McpFactory_WithSseAndNoOptions_CreatesDefaultTransportOptions()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = TransportTypes.Sse,
            Location = "http://localhost:8080"
        };

        var defaultOptions = new SseClientTransportOptions();

        // Act
        using var factory = new McpClientFactory(
            [config],
            _defaultOptions,
            NullLoggerFactory.Instance
        );

        var transport = factory.TransportFactoryMethod(config) as SseClientTransport;

        // Assert
        Assert.NotNull(transport);
        Assert.True(transport.Options.ConnectionTimeout == defaultOptions.ConnectionTimeout);
        Assert.True(transport.Options.MaxReconnectAttempts == defaultOptions.MaxReconnectAttempts);
        Assert.True(transport.Options.ReconnectDelay == defaultOptions.ReconnectDelay);
        Assert.True(transport.Options.AdditionalHeaders == null && defaultOptions.AdditionalHeaders == null);
    }

    [Fact]
    public void McpFactory_WithSseAndMissingOptions_CreatesCorrectTransportOptions()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = TransportTypes.Sse,
            Location = "http://localhost:8080",
            TransportOptions = new Dictionary<string, string>
            {
                ["connectionTimeout"] = "10",
                ["header.test"] = "the_header_value"
            }
        };

        var defaultOptions = new SseClientTransportOptions();

        // Act
        using var factory = new McpClientFactory(
            [config],
            _defaultOptions,
            NullLoggerFactory.Instance
        );

        var transport = factory.TransportFactoryMethod(config) as SseClientTransport;

        // Assert
        Assert.NotNull(transport);
        Assert.Equal(TimeSpan.FromSeconds(10), transport.Options.ConnectionTimeout);
        Assert.Equal(defaultOptions.MaxReconnectAttempts, transport.Options.MaxReconnectAttempts);
        Assert.Equal(defaultOptions.ReconnectDelay, transport.Options.ReconnectDelay);
        Assert.NotNull(transport.Options.AdditionalHeaders);
        Assert.Equal("the_header_value", transport.Options.AdditionalHeaders["test"]);
    }

    [Theory]
    [InlineData("connectionTimeout", "not_a_number")]
    [InlineData("maxReconnectAttempts", "invalid")]
    [InlineData("reconnectDelay", "bad_value")]
    public void McpFactory_WithInvalidTransportOptions_ThrowsFormatException(string key, string value)
    {
        // arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = TransportTypes.Sse,
            Location = "http://localhost:8080",
            TransportOptions = new Dictionary<string, string>
            {
                [key] = value
            }
        };

        // Act
        using var factory = new McpClientFactory(
            [config],
            _defaultOptions,
            NullLoggerFactory.Instance
        );

        // act & assert
        Assert.Throws<FormatException>(() =>
            factory.TransportFactoryMethod(config));
    }
}
