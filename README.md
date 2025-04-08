# WatchedApi

WatchedApi is the backend API for the Watched Movies project. 
It provides endpoints for user authentication, posts, comments, movie ratings, and likes with CRUD operations on each.

## Testing
To run the unit tests and collect code coverage, first navigate to the `WatchedApi.Tests` project directory using:

```bash
coverlet bin/Debug/net8.0/WatchedApi.Tests.dll --target "dotnet" --targetargs "test --no-build" --format cobertura
```
## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQLite
- A terminal (or the integrated terminal in Visual Studio / VS Code)
- (If not installed previously) Entity Framework Core CLI tools  
  Install via:
  ```bash
  dotnet tool install --global dotnet-ef
  ```
  # Navigate to project root
  ```bash
  npm install
  ```
  # Update database with
  ```bash
  dotnet ef database update
  ```
  # Optional
  You can opt to restore, clean and build seperately before running the project
  ```bash
  dotnet restore
  dotnet clean
  dotnet build
  ```

  # Run the project
  ```bash
  dotnet run
  ```

  Use Ctrl+c to shut server down once done testing.
