using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using ReconciliationEngine.API.Middleware;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ReconciliationEngine.Tests.E2E;

public class MiddlewareBehaviorTests
{
    [Fact]
    public async Task CorrelationIdMiddleware_WhenNoHeaderExists_GeneratesNewGuid()
    {
        var httpContext = new DefaultHttpContext();
        
        var middleware = new CorrelationIdMiddleware(next => Task.CompletedTask);
        
        await middleware.InvokeAsync(httpContext);
        
        httpContext.Response.Headers["X-Correlation-Id"].Should().NotBeEmpty();
        
        var correlationId = httpContext.Response.Headers["X-Correlation-Id"].ToString();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CorrelationIdMiddleware_WhenHeaderExists_UsesExisting()
    {
        var existingCorrelationId = Guid.NewGuid().ToString();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Correlation-Id"] = existingCorrelationId;
        
        var middleware = new CorrelationIdMiddleware(next => Task.CompletedTask);
        
        await middleware.InvokeAsync(httpContext);
        
        httpContext.Response.Headers["X-Correlation-Id"].Should().Contain(existingCorrelationId);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_StoresCorrelationIdInContext()
    {
        var httpContext = new DefaultHttpContext();
        
        var middleware = new CorrelationIdMiddleware(next => Task.CompletedTask);
        
        await middleware.InvokeAsync(httpContext);
        
        httpContext.Items.Should().ContainKey("CorrelationId");
    }
}

public class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task GlobalExceptionMiddleware_WhenExceptionThrown_Returns500WithProblemDetails()
    {
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        var middleware = new GlobalExceptionMiddleware(
            next => throw new InvalidOperationException("Test error"),
            loggerMock.Object);

        var httpContext = new DefaultHttpContext();
        
        await middleware.InvokeAsync(httpContext);
        
        httpContext.Response.StatusCode.Should().Be(500);
        httpContext.Response.ContentType.Should().Contain("application/problem+json");
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_WhenExceptionThrown_DoesNotExposeStackTrace()
    {
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        var middleware = new GlobalExceptionMiddleware(
            next => throw new InvalidOperationException("Test error"),
            loggerMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        
        await middleware.InvokeAsync(httpContext);
        
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(httpContext.Response.Body);
        var content = await reader.ReadToEndAsync();
        
        content.Should().NotContain("StackTrace");
        content.Should().NotContain("at System.");
        content.Should().NotContain("at Microsoft.");
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_WhenExceptionThrown_ReturnsCorrelationId()
    {
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        var middleware = new GlobalExceptionMiddleware(
            next => throw new InvalidOperationException("Test error"),
            loggerMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = "test-correlation-id";
        httpContext.Response.Body = new MemoryStream();
        
        await middleware.InvokeAsync(httpContext);
        
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(httpContext.Response.Body);
        var content = await reader.ReadToEndAsync();
        
        content.Should().Contain("test-correlation-id");
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_WhenNoException_PassesThrough()
    {
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        var middleware = new GlobalExceptionMiddleware(
            next => Task.CompletedTask,
            loggerMock.Object);

        var httpContext = new DefaultHttpContext();
        
        await middleware.InvokeAsync(httpContext);
        
        httpContext.Response.StatusCode.Should().Be(200);
    }
}
