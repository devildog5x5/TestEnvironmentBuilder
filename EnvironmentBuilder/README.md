# Environment Builder ⚔️

**Modern test environment creation tool - Evolved from TreeBuilder 3.4**

*Built by Robert Foster - Test Brutally*

![Environment Builder](EnvironmentBuilderApp/Resources/TestTree.svg)

## Overview

Environment Builder is a modern Windows application for creating LDAP directory structures, users, and home directories for test environments. It evolved from the legacy TreeBuilder 3.4 VB6 application with a completely rewritten modern C# WPF codebase.

## Features

### Core Functionality (from TreeBuilder)
- ✅ **LDAP Connection Management** - Connect to LDAP/Active Directory servers
- ✅ **User Bulk Creation** - Create multiple users with customizable templates
- ✅ **Tree/Container Configuration** - Build organizational unit hierarchies
- ✅ **Home Directory Setup** - Create user home directories
- ✅ **LDIF File Generation** - Create LDIF files for import operations
- ✅ **Configuration Save/Load** - Save and restore environment configurations

### New Features
- 🆕 **Modern WPF UI** - Dark theme with Spartan warrior aesthetic
- 🆕 **JSON Configuration** - Modern JSON-based configuration files
- 🆕 **Multiple User Sets** - Support for multiple user configuration sets
- 🆕 **Environment Presets** - Quick templates for common scenarios
- 🆕 **Progress Tracking** - Real-time progress and logging
- 🆕 **Async Operations** - Non-blocking UI during long operations
- 🆕 **Serilog Logging** - Comprehensive file logging

## System Requirements

- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (included in self-contained build)
- Network access to target LDAP server

## Installation

### Option 1: Installer
1. Run `EnvironmentBuilderSetup.exe`
2. Follow the installation wizard
3. Launch from Start Menu or Desktop shortcut

### Option 2: Portable
1. Extract the publish folder contents
2. Run `EnvironmentBuilderApp.exe`

## Quick Start

1. **Connection Tab**: Enter your LDAP server details
   - Server Address (IP or hostname)
   - Port (389 for LDAP, 636 for LDAPS)
   - Bind DN (admin username)
   - Password

2. **Users Tab**: Configure user creation
   - Set username prefix (e.g., "TestUser")
   - Define start/end numbers
   - Set default password
   - Specify user context (container DN)

3. **Output Tab**: Configure LDIF output
   - Set export file path
   - Choose write-only or execute mode

4. **Run Update**: Click to build your environment!

## Configuration File Format

Configurations are saved as JSON files:

```json
{
  "Name": "Test Environment",
  "Connection": {
    "ServerAddress": "ldap.example.com",
    "Port": 389,
    "BindDN": "cn=admin,o=org"
  },
  "UserConfigs": [
    {
      "SetName": "Test Users",
      "UserNamePrefix": "TestUser",
      "StartNumber": 1,
      "EndNumber": 100,
      "Password": "Password123!"
    }
  ]
}
```

## Building from Source

### Prerequisites
- Visual Studio 2022 or VS Code
- .NET 8.0 SDK

### Build Commands
```powershell
cd EnvironmentBuilder
dotnet restore
dotnet build
dotnet publish -c Release -r win-x64 --self-contained true
```

### Creating Installer
1. Install [Inno Setup](https://jrsoftware.org/isinfo.php)
2. Open `Installer/EnvironmentBuilderInstaller.iss`
3. Compile to create the installer

## Project Structure

```
EnvironmentBuilder/
├── EnvironmentBuilderApp/
│   ├── Models/
│   │   ├── ConnectionSettings.cs
│   │   ├── UserConfiguration.cs
│   │   ├── TreeConfiguration.cs
│   │   └── EnvironmentConfiguration.cs
│   ├── Services/
│   │   ├── LdapService.cs
│   │   └── LdifService.cs
│   ├── ViewModels/
│   │   └── MainViewModel.cs
│   ├── Resources/
│   │   └── TestTree.svg
│   ├── MainWindow.xaml
│   └── App.xaml
├── Installer/
│   └── EnvironmentBuilderInstaller.iss
├── publish/
└── README.md
```

## Technology Stack

- **Framework**: .NET 8.0 WPF
- **LDAP**: System.DirectoryServices.Protocols
- **MVVM**: CommunityToolkit.Mvvm
- **Serialization**: Newtonsoft.Json
- **Logging**: Serilog

## History

This project is a modernization of TreeBuilder 3.4, originally created in Visual Basic 6 for creating Novell NDS/eDirectory test environments via ICE.EXE and LDIF files. The new version uses modern .NET technologies while preserving the core functionality.

## License

Copyright © 2024 Robert Foster. All rights reserved.

## Acknowledgments

- Original TreeBuilder 3.4 architecture
- Test Brutally: *Build Your Level of Complexity*

