# 2048-Maui
The classic 2048 game built with .NET MAUI

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- .NET MAUI workload

## Setup

1. **Install .NET MAUI workload:**
   ```bash
   dotnet workload install maui
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore 2048-Maui.slnx
   ```

3. **Build the solution:**
   ```bash
   dotnet build 2048-Maui.slnx
   ```

4. **Run tests:**
   ```bash
   dotnet test 2048-Maui.slnx
   ```

## Project Structure

This project uses modern .NET scaffolding:

- **slnx format**: New XML-based solution file format for .NET 10
- **Central Package Management (CPM)**: Package versions managed centrally in `Directory.Packages.props`
- **Consolidated props**: Common build properties defined in `Directory.Build.props`
- **MSTest.Sdk**: Modern test project format with integrated test runner
- **src/**: Source code projects
- **test/**: Test projects

## Technologies

- .NET 10
- .NET MAUI for cross-platform UI
- MSTest for unit testing
- Moq 4.18 for mocking

## CI/CD

The project includes a GitHub Actions workflow that automatically builds and tests the solution on every push and pull request.

