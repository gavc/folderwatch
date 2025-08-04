# FolderWatch WPF Application

A modern WPF application for monitoring folders and applying automated rules to files using MahApps.Metro UI framework.

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Windows 10/11

### Running the Application

**Important**: The application must be run from the `FolderWatch.WPF` folder to function correctly.

```powershell
# Navigate to the project folder first
cd FolderWatch.WPF

# Then run the application
dotnet run
```

Or use the combined command:
```powershell
cd c:\temp\folderwatch\FolderWatch.WPF; dotnet run
```

### Building the Application

```powershell
# Build from the project folder
cd FolderWatch.WPF
dotnet build

# Or build the entire solution from root
dotnet build folderwatch.sln
```

### Properly Exiting the Application

The application is designed to minimize to the system tray when you click the X button. To properly exit:

**Method 1: Tray Icon (Recommended)**
1. Right-click the system tray icon
2. Select "Exit" from the context menu

**Method 2: File Menu**
1. Use the File menu → Exit

**Method 3: Keyboard Shortcuts**
- `Alt + F4` (standard Windows shortcut)
- `Ctrl + Q` (alternative shortcut)

**If the application doesn't exit properly**, you can force terminate it:
```powershell
taskkill /F /IM "FolderWatch.WPF.exe"
```

## Features

- **Modern Metro UI** - Built with MahApps.Metro for a clean, modern interface
- **File Monitoring** - Monitor specified folders for file changes
- **Rule-Based Processing** - Apply custom rules to files (Move, Copy, Delete, Rename)
- **System Tray Integration** - Minimize to system tray with notifications
- **Theme Support** - Light/Dark themes with multiple accent colors
- **Settings Management** - Configurable application settings

## Architecture

### MVVM Pattern
- **Models**: `Rule`, `RuleStep`, `AppSettings`
- **ViewModels**: `MainViewModel`, `SettingsViewModel`
- **Views**: `MainWindow`, `SettingsWindow`

### Services
- **IFileMonitorService**: File system monitoring with temporary file detection
- **IRuleService**: Rule persistence and management
- **ISettingsService**: Application settings management
- **IThemeService**: MahApps.Metro theme management

### Key Components
- **FileAccessibilityChecker**: Handles temporary files (.tmp, .crdownload) with retry logic
- **PatternMatcher**: Glob pattern matching for file filtering
- **ViewModelBase**: MVVM base class with INotifyPropertyChanged
- **RelayCommand**: ICommand implementation

## Current Status

✅ **Working Features:**
- Project builds successfully
- Modern Metro UI loads
- Add/Delete rules with immediate UI updates
- Settings persistence (JSON)
- Theme switching
- System tray integration (designed)

🔧 **Recently Fixed:**
- Thread-safety issues with ObservableCollections
- UI updates now happen on UI thread
- Button commands refresh properly
- XAML resource issues resolved

⚠️ **Known Issues:**
- Settings dialog not fully implemented
- Rule editing dialog needs implementation
- File monitoring may have threading issues
- Some advanced features are placeholders

## Technology Stack

- **.NET 8** with C# 13 features
- **WPF** for desktop UI
- **MahApps.Metro** 2.4.10 for modern styling
- **Microsoft.Extensions.DependencyInjection** for IoC
- **Microsoft.Extensions.Hosting** for application lifecycle
- **Newtonsoft.Json** for settings serialization
- **Hardcodet.NotifyIcon.Wpf** for system tray

## Getting Started

### Prerequisites
- .NET 8 SDK
- Windows 10/11

### Building
```bash
git clone https://github.com/gavc/folderwatch.git
cd folderwatch
dotnet build
```

### Running
```bash
cd FolderWatch.WPF
dotnet run
```

## Project Structure

```
FolderWatch.WPF/
├── Models/
│   ├── AppSettings.cs
│   ├── Rule.cs
│   └── RuleStep.cs
├── ViewModels/
│   ├── MainViewModel.cs
│   └── SettingsViewModel.cs
├── Views/
│   ├── MainWindow.xaml
│   └── SettingsWindow.xaml
├── Services/
│   ├── FileMonitorService.cs
│   ├── RuleService.cs
│   ├── SettingsService.cs
│   └── ThemeService.cs
├── Helpers/
│   ├── ViewModelBase.cs
│   ├── RelayCommand.cs
│   └── PatternMatcher.cs
└── Resources/
```

## Development Notes

### Thread Safety
All UI collection updates are dispatched to the UI thread using:
```csharp
await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
    // UI updates here
});
```

### Command Pattern
Commands use RelayCommand with proper CanExecute logic:
```csharp
AddRuleCommand = new RelayCommand(AddRule);
DeleteRuleCommand = new RelayCommand<Rule>(async rule => await DeleteRuleAsync(rule), 
    rule => rule is not null);
```

### Settings Management
Settings are persisted as JSON and loaded asynchronously:
```csharp
await _settingsService.LoadSettingsAsync();
await _settingsService.SaveSettingsAsync();
```

## Contributing

1. Follow C# naming conventions (PascalCase for public members)
2. Use async/await for I/O operations
3. Ensure UI updates happen on UI thread
4. Add XML documentation for public APIs
5. Follow MVVM pattern separation of concerns

## License

This project is open source. See LICENSE file for details.
