# Medical Equipment Order Processing System

A robust C# application for processing medical equipment orders with a focus on SOLID principles, testability, and maintainable code.

## Overview

This system handles medical equipment order processing, including order management, delivery notifications, and alert services. Built with modern C# practices and a strong emphasis on clean architecture.

## Key Features

### Architecture Improvements

#### SOLID Principles Implementation
- **Single Responsibility**: Separated into specialized components
  - OrderRepository: Handles order data access
  - AlertService: Manages notification system
  - OrderProcessor: Coordinates order processing logic
- **Open/Closed**: Interface-based design enables extension without modification
- **Dependency Inversion**: Utilizes dependency injection throughout
- **Interface Segregation**: Focused interfaces for specific responsibilities
  - IOrderProcessor
  - IOrderRepository
  - IAlertService
  - IHttpClientWrapper

#### Testing Enhancements
- Injectable dependencies for all components
- IHttpClientWrapper interface for HTTP call mocking
- Immutable record types for data models
- Pure functions for predictable behavior and testing
- Comprehensive test coverage capabilities

#### DRY (Don't Repeat Yourself) Improvements
- Centralized HTTP client handling
- Reusable configuration patterns
- Streamlined string content creation
- Consistent approach to JSON handling

## Solution Structure

### Project Organization
- Core business logic consolidated in main project
- Separate test project within the same solution
- Clear separation of concerns throughout

### Testing Infrastructure
- **Frameworks & Tools**:
  - xUnit: Primary testing framework
  - AutoFixture: Test data generation
  - Moq: Dependency mocking
  - FluentAssertions: Readable test assertions

### Test Implementation
- Constructor-based test setup
- Follows Arrange-Act-Assert pattern
- Comprehensive mocking examples 
- Coverage of both success and failure scenarios

## Getting Started

1. Clone the repository
2. Restore NuGet packages
3. Build the solution
4. Run tests to verify setup

## Dependencies

- .NET Core 8.0+
- Newtonsoft.Json
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Http

### Test Dependencies
- xUnit
- Moq
- AutoFixture
- FluentAssertions

## Configuration

The system uses dependency injection for configuration. Key settings include:
- Order API URL
- Update API URL
- Alert API URL

## Best Practices Implemented

1. **Interface-Based Design**
   - Enables easy mocking for tests
   - Facilitates future extensions
   - Clear contract definitions

2. **Immutable Data Models**
   - Prevents unexpected state changes
   - Thread-safe by design
   - Predictable behavior

3. **HTTP Client Management**
   - Centralized HTTP handling
   - Proper disposal of resources
   - Consistent error handling

4. **Testing Strategy**
   - Unit tests for each component
   - Integration test capabilities
   - Mocking of external dependencies

## Testing Examples

```csharp
[Fact]
public async Task ProcessOrders_WithDeliveredItems_SendsAlerts()
{
    // Arrange
    var fixture = new Fixture();
    var mockRepository = new Mock<IOrderRepository>();
    var mockAlertService = new Mock<IAlertService>();
    
    // Test implementation details...
}
```
