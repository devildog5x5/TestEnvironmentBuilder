# Environment Builder ⚔️🛡️

**Modern test environment creation tool - Evolved from TreeBuilder 3.4**

*Built by Robert Foster - Test Brutally - Build Your Level of Complexity*

## Overview

Environment Builder is a comprehensive suite for creating LDAP directory structures, users, and home directories for test environments. It evolved from the legacy TreeBuilder 3.4 VB6 application into a modern multi-project solution with CLI, REST API, Web Dashboard, and WPF desktop application.

## 🏗️ Project Components

| Component | Description | Technology |
|-----------|-------------|------------|
| **EnvironmentBuilderApp** | Desktop GUI application | WPF (.NET 8.0) |
| **EnvironmentBuilder.CLI** | Command-line interface | Console (.NET 8.0) |
| **EnvironmentBuilder.API** | REST API with real-time updates | ASP.NET Core + SignalR |
| **EnvironmentBuilder.Web** | Web Dashboard | Blazor Server |
| **EnvironmentBuilder.Core** | Shared library | .NET 8.0 Class Library |

## ✨ Features

### Core Functionality
- ✅ **LDAP Connection Management** - Connect to LDAP/Active Directory servers
- ✅ **User Bulk Creation** - Create multiple users with realistic test data
- ✅ **Tree/Container Configuration** - Build organizational unit hierarchies
- ✅ **Home Directory Setup** - Create user home directories
- ✅ **LDIF File Generation** - Create LDIF files for import operations
- ✅ **Configuration Save/Load** - Save and restore environment configurations

### Complexity Presets
| Preset | Users | Description |
|--------|-------|-------------|
| 🟢 **Simple** | 10 | Quick setup for basic testing |
| 🔵 **Medium** | 100 | Standard test environment with nested OUs |
| 🟡 **Complex** | 1,000 | Enterprise-scale testing with deep hierarchy |
| 🔴 **Brutal** | 10,000+ | Stress test with maximum complexity |

### New Features
- 🆕 **Realistic Data Generation** - Uses Bogus library for realistic user data
- 🆕 **CLI Support** - Full command-line interface for automation
- 🆕 **REST API** - Programmatic control via REST endpoints
- 🆕 **Web Dashboard** - Browser-based monitoring and control
- 🆕 **Real-time Progress** - SignalR-powered live updates
- 🆕 **Health Checks** - Validate server connectivity
- 🆕 **Cleanup/Teardown** - Remove test users after testing
- 🆕 **HTML/CSV/JSON Reports** - Export detailed reports
- 🆕 **Parallel Operations** - Configurable parallelism for speed
- 🆕 **User Archetypes** - Generate executives, developers, support staff, etc.

## 🚀 Quick Start

### Option 1: Desktop App
```powershell
cd EnvironmentBuilder
dotnet run --project EnvironmentBuilderApp
```

### Option 2: CLI
```powershell
# Build a simple environment
envbuilder build --preset simple --server localhost --port 389 --bind-dn "cn=admin,o=org" --password secret

# Build 1000 users with realistic data
envbuilder build --preset complex --users 1000 --prefix testuser --dry-run

# Cleanup test users
envbuilder cleanup --prefix testuser --force

# Check server health
envbuilder health --server ldap.example.com
```

### Option 3: REST API
```powershell
# Start the API
dotnet run --project EnvironmentBuilder.API

# POST to start a build
curl -X POST http://localhost:5000/api/environment/build \
  -H "Content-Type: application/json" \
  -d '{"preset":"medium","server":"localhost","userCount":100}'
```

### Option 4: Web Dashboard
```powershell
# Start both API and Web
dotnet run --project EnvironmentBuilder.API &
dotnet run --project EnvironmentBuilder.Web
# Open http://localhost:5001
```

## 📦 Installation

### Prerequisites
- Windows 10/11 (64-bit)
- .NET 8.0 SDK or Runtime

### From Installer
1. Run `EnvironmentBuilderSetup.exe`
2. Follow the installation wizard
3. Launch from Start Menu or Desktop shortcut

### From Source
```powershell
git clone https://github.com/devildog5x5/TestEnvironmentBuilder.git
cd TestEnvironmentBuilder/EnvironmentBuilder
dotnet restore
dotnet build
```

## 🔧 Configuration

### JSON Configuration File
```json
{
  "Name": "Test Environment",
  "ComplexityLevel": "Medium",
  "Connection": {
    "Server": "ldap.example.com",
    "Port": 389,
    "BindDn": "cn=admin,o=org",
    "BaseDn": "o=org",
    "UseSsl": false
  },
  "Users": {
    "Count": 100,
    "Prefix": "testuser",
    "RandomizeData": true,
    "DefaultPassword": "Test123!"
  },
  "Execution": {
    "BatchSize": 25,
    "ParallelOperations": 4,
    "DryRun": false
  }
}
```

### CLI Configuration
```powershell
# Create a configuration file
envbuilder config init --preset complex --output myenv.json

# Show configuration
envbuilder config show --file myenv.json
```

## 📊 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/environment/build` | Start a build operation |
| POST | `/api/environment/cleanup` | Start a cleanup operation |
| GET | `/api/environment/operations` | List all operations |
| GET | `/api/environment/operations/{id}` | Get operation status |
| POST | `/api/environment/operations/{id}/cancel` | Cancel operation |
| POST | `/api/environment/health` | Perform health check |
| GET | `/api/environment/presets` | Get available presets |

### SignalR Hub
Connect to `/hubs/progress` for real-time updates:
- `ProgressUpdate` - Current progress
- `LogMessage` - Log entries
- `OperationComplete` - Completion notification

## 📁 Project Structure

```
EnvironmentBuilder/
├── EnvironmentBuilder.sln
├── EnvironmentBuilder.Core/         # Shared library
│   ├── Models/
│   │   ├── ComplexityPreset.cs
│   │   ├── EnvironmentConfig.cs
│   │   ├── OperationResult.cs
│   │   └── TestUser.cs
│   └── Services/
│       ├── EnvironmentService.cs
│       ├── TestDataGenerator.cs
│       └── ReportService.cs
├── EnvironmentBuilder.CLI/          # Command-line tool
│   ├── Commands/
│   └── Program.cs
├── EnvironmentBuilder.API/          # REST API
│   ├── Controllers/
│   ├── Hubs/
│   └── Services/
├── EnvironmentBuilder.Web/          # Blazor Dashboard
│   └── Components/Pages/
├── EnvironmentBuilderApp/           # WPF Desktop App
│   ├── Models/
│   ├── Services/
│   ├── ViewModels/
│   └── MainWindow.xaml
├── Installer/
│   └── EnvironmentBuilderInstaller.iss
└── README.md
```

## 🛠️ Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 8.0 |
| Desktop UI | WPF |
| Web UI | Blazor Server |
| API | ASP.NET Core |
| Real-time | SignalR |
| Data Generation | Bogus |
| LDAP | System.DirectoryServices.Protocols |
| MVVM | CommunityToolkit.Mvvm |
| CLI | System.CommandLine |
| Console UI | Spectre.Console |
| Serialization | Newtonsoft.Json |
| Logging | Serilog |

## 📜 History

This project is a modernization of TreeBuilder 3.4, originally created in Visual Basic 6 for creating Novell NDS/eDirectory test environments via ICE.EXE and LDIF files. The new version uses modern .NET technologies while preserving and expanding the core functionality.

## 📄 License

Copyright © 2024 Robert Foster. All rights reserved.

---

**Test Brutally - Build Your Level of Complexity** 🛡️⚔️
