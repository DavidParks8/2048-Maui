# Test Project Guidelines

## Overview

This folder contains unit tests for the 2048 MAUI application. Tests are written using MSTest and run in parallel for efficiency.

## Running Tests

**Always specify a project when running `dotnet test`**. Never run `dotnet test` without the `--project` argument, as this will attempt to run all test projects including Appium tests which require a running simulator.

```bash
# Correct - specify the project
dotnet test --project test/TwentyFortyEight.Core.Tests
dotnet test --project test/TwentyFortyEight.ViewModels.Tests

# Wrong - do not do this
dotnet test
```

## Test Framework

- **MSTest SDK**: Tests use the MSTest.Sdk project style for simplified configuration
- **NSubstitute**: Available for substituting interfaces and dependencies
- **Parallel Execution**: Tests run in parallel at the method level with `[Parallelize(Scope = ExecutionScope.MethodLevel, Workers = 0)]`

## SDK-Style Test Projects

New test projects should use the `MSTest.Sdk` project style for minimal configuration. This SDK automatically includes the MSTest framework packages and enables modern testing features.

```xml
<Project Sdk="MSTest.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NSubstitute" />
  </ItemGroup>

</Project>
```

Benefits of `MSTest.Sdk`:

- No need to manually reference `Microsoft.NET.Test.Sdk`, `MSTest.TestAdapter`, or `MSTest.TestFramework`
- Automatic configuration of test runner and assertions
- Simplified project file with minimal boilerplate
- Built-in support for code coverage and parallel execution

## Writing Tests

### Naming Convention

Use descriptive method names following the pattern: `MethodName_Scenario_ExpectedResult`

```csharp
[TestMethod]
public void MoveLeft_MergesMultiplePairs()
{
    // ...
}
```

### Test Structure

Follow the Arrange-Act-Assert pattern:

```csharp
[TestMethod]
public void ExampleTest()
{
    // Arrange
    Game2048Engine engine = new(new GameConfig(), new SystemRandomSource());

    // Act
    var moved = engine.Move(Direction.Left);

    // Assert
    Assert.IsTrue(moved);
}
```

### Using Substitutes

Use `SystemRandomSource(seed)` for deterministic tests or NSubstitute for precise control:

```csharp
// Seeded random for deterministic tests
var random = new SystemRandomSource(42);
Game2048Engine engine = new(config, random);

// Or use NSubstitute for precise control
IRandomSource randomSubstitute = Substitute.For<IRandomSource>();
randomSubstitute.Next(Arg.Any<int>()).Returns(0);
randomSubstitute.NextDouble().Returns(0.5);

Game2048Engine substitutedEngine = new(config, randomSubstitute);
```
