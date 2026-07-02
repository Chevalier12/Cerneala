# MonoGame Scratch Init Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Initialize this repository as a root .NET library with MonoGame 3.8.4.1 and a nested `Playground` MonoGame application that references it.

**Architecture:** The root project is the reusable `Cerneala` class library. `Playground/Cerneala.Playground` is a DesktopGL app that depends on the root library through a project reference.

**Tech Stack:** .NET 8 target framework, MonoGame.Framework.DesktopGL 3.8.4.1, MonoGame.Templates.CSharp 3.8.4.1, xUnit for the root library smoke test.

## Global Constraints

- `Cerneala.csproj` must live directly in the repository root.
- MonoGame package version must be `3.8.4.1`.
- `Playground` must be inside this repository.
- Keep the initial API minimal and avoid extra game framework abstractions.

---

### Task 1: Root Library and Test Harness

**Files:**
- Create: `Cerneala.slnx`
- Create: `Cerneala.csproj`
- Create: `tests/Cerneala.Tests/Cerneala.Tests.csproj`
- Create: `tests/Cerneala.Tests/GameBootstrapTests.cs`

**Interfaces:**
- Produces: `Cerneala.GameBootstrap.CreateDefaultClearColor(): Microsoft.Xna.Framework.Color`

- [ ] **Step 1: Initialize git, solution, root class library, and test project**

Run:
```powershell
git init
dotnet new sln -n Cerneala
dotnet new classlib -n Cerneala -o . --framework net8.0
dotnet new xunit -n Cerneala.Tests -o tests/Cerneala.Tests --framework net8.0
dotnet sln Cerneala.slnx add Cerneala.csproj
dotnet sln Cerneala.slnx add tests/Cerneala.Tests/Cerneala.Tests.csproj
dotnet add tests/Cerneala.Tests/Cerneala.Tests.csproj reference Cerneala.csproj
```

- [ ] **Step 2: Add the failing test**

Create `tests/Cerneala.Tests/GameBootstrapTests.cs`:
```csharp
using Microsoft.Xna.Framework;

namespace Cerneala.Tests;

public sealed class GameBootstrapTests
{
    [Fact]
    public void CreateDefaultClearColorReturnsCornflowerBlue()
    {
        Assert.Equal(Color.CornflowerBlue, GameBootstrap.CreateDefaultClearColor());
    }
}
```

- [ ] **Step 3: Verify RED**

Run:
```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj
```

Expected: fail because `Microsoft.Xna.Framework` and `GameBootstrap` are not available yet.

- [ ] **Step 4: Add MonoGame 3.8.4.1 to the library and test project**

Run:
```powershell
dotnet add Cerneala.csproj package MonoGame.Framework.DesktopGL --version 3.8.4.1
dotnet add tests/Cerneala.Tests/Cerneala.Tests.csproj package MonoGame.Framework.DesktopGL --version 3.8.4.1
```

- [ ] **Step 5: Implement minimal library API**

Create `GameBootstrap.cs`:
```csharp
using Microsoft.Xna.Framework;

namespace Cerneala;

public static class GameBootstrap
{
    public static Color CreateDefaultClearColor()
    {
        return Color.CornflowerBlue;
    }
}
```

Delete generated `Class1.cs`.

- [ ] **Step 6: Verify GREEN**

Run:
```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj
```

Expected: pass.

### Task 2: Playground MonoGame Application

**Files:**
- Create: `Playground/Cerneala.Playground/Cerneala.Playground.csproj`
- Modify: `Playground/Cerneala.Playground/Game1.cs`

**Interfaces:**
- Consumes: `Cerneala.GameBootstrap.CreateDefaultClearColor(): Microsoft.Xna.Framework.Color`
- Produces: a runnable DesktopGL playground project.

- [ ] **Step 1: Install MonoGame templates**

Run:
```powershell
dotnet new install MonoGame.Templates.CSharp::3.8.4.1
```

- [ ] **Step 2: Create Playground from the DesktopGL template**

Run:
```powershell
dotnet new mgdesktopgl -n Cerneala.Playground -o Playground/Cerneala.Playground
dotnet sln Cerneala.slnx add Playground/Cerneala.Playground/Cerneala.Playground.csproj
dotnet add Playground/Cerneala.Playground/Cerneala.Playground.csproj reference Cerneala.csproj
```

- [ ] **Step 3: Use the root library from Playground**

Update `Playground/Cerneala.Playground/Game1.cs` so `Draw` clears with:
```csharp
GraphicsDevice.Clear(GameBootstrap.CreateDefaultClearColor());
```

- [ ] **Step 4: Verify the whole solution**

Run:
```powershell
dotnet restore Cerneala.slnx
dotnet test Cerneala.slnx
dotnet build Cerneala.slnx --no-restore
```

Expected: restore, tests, and build pass.
