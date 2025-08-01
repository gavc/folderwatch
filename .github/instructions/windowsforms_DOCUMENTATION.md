# FolderWatcher Application - Complete Documentation

## Application Overview

**FolderWatcher** is a Windows Forms-based file monitoring and processing application built with .NET 8 and C# 13. It provides real-time folder monitoring capabilities with rule-based file processing, multi-step workflows, and comprehensive system tray integration.

### THIS SHOULD BE USED AS A GUIDE ONLY, THE WPF VERSION SHOULD USE MODERN WPF METHODS AND PATTERNS.

### Key Features
- **Real-time Folder Monitoring**: FileSystemWatcher-based file detection
- **Rule-Based Processing**: Pattern matching with glob-style wildcards
- **Multi-Step Workflows**: Complex file operations with conditional logic
- **File Actions**: Copy, Move, Rename, Delete operations
- **Advanced Renaming**: Date/time formatting and sequential numbering
- **System Tray Integration**: Background operation with tray notifications
- **Robust File Handling**: Temporary file detection and retry logic
- **Comprehensive Help System**: Context-sensitive documentation
- **Material Design UI**: Modern interface using ReaLTaiizor components

## Architecture Overview

### Application Structure
```
FolderWatcher/
├── Program.cs                  # Application entry point
├── MainForm.cs/.Designer.cs    # Primary UI and file monitoring logic
├── RuleStepsForm.cs/.Designer.cs  # Multi-step rule editor
├── RuleStepForm.cs/.Designer.cs   # Individual step configuration
├── SettingsForm.cs/.Designer.cs   # System tray preferences
├── HelpViewerForm.cs/.Designer.cs # Help system viewer
├── Models/
│   ├── Rule.cs                 # Rule data model
│   ├── RuleStep.cs             # Individual step model
│   └── AppSettings.cs          # Application settings
├── Services/
│   ├── RuleManager.cs          # Thread-safe rule management
│   ├── FileAccessibilityChecker.cs  # Robust file handling
│   └── ThemeManager.cs         # Application theming
└── Tests/
    └── FileHandlingTests.cs    # Unit tests for file operations
```

## Core Components

### 1. MainForm - Primary Application Interface

**Purpose**: Main application window with tabbed interface for rules, logging, and live monitoring.

**Key Features**:
- **Folder Selection**: Browse and select folders to monitor
- **Rule Management**: ListView-based rule configuration with drag-and-drop reordering
- **Real-time Monitoring**: Start/stop folder watching with status indicators
- **Dual Logging System**: 
  - Rule Log: Historical rule applications
  - Live Log: Real-time activity with optional filtering
- **System Tray Integration**: Minimize to tray, background operation
- **Context Menus**: Right-click operations for rules and tray

**UI Layout**:
- **Header**: Folder path textbox with browse button
- **Tabbed Interface**:
  - Rules Tab: Rule list with management buttons
  - Log Tab: Historical rule application log
  - Live Log Tab: Real-time activity with enable/disable toggle
- **Footer**: Start/Stop buttons with status indicator
- **Menu System**: Tools menu with settings, Help menu with comprehensive documentation

**Code Architecture**:
```csharp
public partial class MainForm : Form
{
    private readonly FileSystemWatcher _watcher;
    private readonly RuleManager _ruleManager;
    private AppSettings _appSettings;
    private bool _liveLogEnabled = true;
    private bool _isExiting = false;

    // Event handlers for file system monitoring
    private void OnFileCreated(object? sender, FileSystemEventArgs e)
    private void OnFileRenamed(object? sender, RenamedEventArgs e)
    
    // Rule management methods
    private async Task LoadRules()
    private void ApplyRule(Rule rule, string filePath)
    
    // System tray integration
    private void InitializeTrayIcon()
    private void MainForm_Resize(object? sender, EventArgs e)
    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
}
```

### 2. Rule System - Core Business Logic

**Rule Class**:
```csharp
public class Rule
{
    public string Name { get; set; } = "";
    public string Pattern { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public RuleAction Action { get; set; } = RuleAction.Move;
    public string Destination { get; set; } = "";
    public string NewName { get; set; } = "";
    public List<RuleStep> Steps { get; set; } = new();
}
```

**RuleStep Class** (Multi-step workflows):
```csharp
public class RuleStep
{
    public RuleAction Action { get; set; }
    public string Destination { get; set; } = "";
    public string NewName { get; set; } = "";
    public bool Enabled { get; set; } = true;
}
```

**Supported Actions**:
- **Copy**: Duplicate file to destination
- **Move**: Relocate file to destination  
- **Rename**: Change filename with advanced patterns
- **Delete**: Remove file permanently
- **DateTime**: Insert current date/time in filename
- **Numbering**: Add sequential numbers to prevent conflicts

### 3. RuleStepsForm - Multi-Step Rule Editor

**Purpose**: Advanced rule editor for creating complex multi-step file processing workflows.

**Features**:
- **Step Management**: Add, edit, remove, and reorder processing steps
- **Visual Step List**: Clear representation of workflow sequence
- **Step Configuration**: Each step has independent action, destination, and naming
- **Validation**: Ensures step configurations are valid before saving

**Workflow Example**:
1. Step 1: Copy file to backup folder
2. Step 2: Rename with date prefix
3. Step 3: Move to processed folder

### 4. File Processing Engine

**FileAccessibilityChecker Class**:
```csharp
public static class FileAccessibilityChecker
{
    // Detects temporary/incomplete downloads
    public static bool IsTemporaryFile(string filePath)
    
    // Checks if file is accessible (not locked)
    public static bool IsFileAccessible(string filePath)
    
    // Retry logic with exponential backoff
    public static async Task<FileAccessResult> WaitForFileAccessAsync(
        string filePath, int maxRetries, int initialDelayMs, ...)
    
    // Comprehensive readiness check
    public static FileReadinessResult CheckFileReadiness(string filePath, ...)
}
```

**Temporary File Extensions Detected**:
- Browser downloads: `.crdownload`, `.download`, `.part`, `.partial`
- Torrent clients: `.!ut`, `.bc!`  
- Generic temporary: `.tmp`, `.temp`
- Browser-specific: `.opdownload`, `.safari-download`

### 5. RuleManager - Thread-Safe Rule Management

**Purpose**: Manages rule persistence and thread-safe access for concurrent file processing.

**Key Features**:
- **Thread-Safe Operations**: ReaderWriterLockSlim for concurrent access
- **JSON Persistence**: Automatic rule saving/loading
- **Event Logging**: Detailed operation logging for debugging
- **Error Handling**: Graceful handling of file I/O errors

```csharp
public class RuleManager : IDisposable
{
    private readonly ReaderWriterLockSlim _rulesLock = new();
    private List<Rule> _rules = new();
    
    public void ExecuteWithReadLock(Action<List<Rule>> action)
    public async Task SaveRulesAsync()
    public async Task LoadRulesAsync()
}
```

### 6. System Tray Integration

**AppSettings Class** (Tray Preferences):
```csharp
public class AppSettings
{
    public bool MinimizeToTray { get; set; } = true;
    public bool StartMinimizedToTray { get; set; } = false;
    public bool ShowTrayNotifications { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public bool CloseToTray { get; set; } = false;
    public FileRetrySettings FileRetrySettings { get; set; } = new();
}
```

**Tray Context Menu**:
- **Open**: Restore main window
- **Start/Stop Watching**: Toggle monitoring with folder name display
- **Settings**: Open preferences dialog
- **Exit**: Terminate application

**Notification System**:
- **Balloon Tips**: Start/stop notifications, background monitoring alerts
- **Status Updates**: Real-time monitoring status in tray tooltip

### 7. Help System

**HelpViewerForm**: Rich text help viewer with search and navigation
- **Context-Sensitive Help**: F1 key support throughout application
- **Comprehensive Documentation**: User guide, wildcards, file actions, use cases, troubleshooting
- **Search Functionality**: Find text within help documents
- **RTF Support**: Rich text formatting for readable documentation

**Help Topics**:
- **User Guide**: Complete application walkthrough
- **Wildcards & Patterns**: File matching syntax (*.txt, photo_*.jpg, etc.)
- **File Actions**: Detailed action explanations with examples
- **Common Use Cases**: Download organization, photo sorting, document filing
- **Troubleshooting**: Common issues and solutions

## Data Models

### Rule Model
```csharp
public class Rule
{
    public string Name { get; set; } = "";
    public string Pattern { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public RuleAction Action { get; set; } = RuleAction.Move;
    public string Destination { get; set; } = "";
    public string NewName { get; set; } = "";
    public List<RuleStep> Steps { get; set; } = new();
}

public enum RuleAction
{
    Copy,
    Move,
    Rename,
    Delete,
    DateTime,
    Numbering
}
```

### Application Settings Model
```csharp
public class AppSettings
{
    // System Tray Preferences
    public bool MinimizeToTray { get; set; } = true;
    public bool StartMinimizedToTray { get; set; } = false;
    public bool ShowTrayNotifications { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public bool CloseToTray { get; set; } = false;
    
    // File Processing Configuration
    public FileRetrySettings FileRetrySettings { get; set; } = new();
    
    // Persistence
    public static async Task<AppSettings> LoadAsync()
    public async Task SaveAsync()
}

public class FileRetrySettings
{
    public int MaxRetries { get; set; } = 3;
    public int InitialDelayMs { get; set; } = 1000;
    public int MaxDelayMs { get; set; } = 5000;
    public double BackoffMultiplier { get; set; } = 2.0;
    public bool SkipTemporaryFiles { get; set; } = true;
}
```

## Service Layer

### RuleManager Service
```csharp
public class RuleManager : IDisposable
{
    // Thread-safe rule collection management
    private readonly ReaderWriterLockSlim _rulesLock = new();
    private List<Rule> _rules = new();
    
    // Public API
    public List<Rule> Rules { get; private set; }
    public event Action<string>? RuleActionLogged;
    
    // Core Operations
    public async Task LoadRulesAsync()
    public async Task SaveRulesAsync()
    public void ExecuteWithReadLock(Action<List<Rule>> action)
    public void ExecuteWithWriteLock(Action<List<Rule>> action)
}
```

### FileAccessibilityChecker Service
```csharp
public static class FileAccessibilityChecker
{
    // Configuration
    public static class DefaultRetryConfig
    {
        public const int MaxRetries = 3;
        public const int InitialDelayMs = 1000;
        public const int MaxDelayMs = 5000;
        public const double BackoffMultiplier = 2.0;
    }
    
    // Core Methods
    public static bool IsTemporaryFile(string filePath)
    public static bool IsFileAccessible(string filePath)
    public static FileReadinessResult CheckFileReadiness(string filePath, Action<string>? logAction = null)
    public static async Task<FileAccessResult> WaitForFileAccessAsync(...)
}

// Result Types
public record FileAccessResult(bool IsAccessible, string Message, int RetriesUsed);
public record FileReadinessResult(bool IsReady, FileNotReadyReason Reason, string Message);

public enum FileNotReadyReason
{
    None,
    InvalidPath,
    TemporaryFile,
    FileNotFound,
    FileNotAccessible
}
```

## User Interface Components

### MainForm Layout
```
┌─────────────────────────────────────────────────┐
│ [File] [Tools] [Help]                           │ ← Menu Bar
├─────────────────────────────────────────────────┤
│ Folder Path: [C:\Downloads            ] [Browse]│ ← Folder Selection
├─────────────────────────────────────────────────┤
│ ┌─[Rules]─[Log]─[Live Log]─────────────────────┐ │
│ │ Rules Tab:                                   │ │
│ │ ┌───────────────────────────────────────────┐ │ │
│ │ │☑ Download Organizer  │*.pdf │Move│Downloads││ │ ← Rules ListView
│ │ │☑ Photo Sorter        │*.jpg │Copy│Photos  ││ │
│ │ │☐ Archive Cleaner     │*.zip │Del │        ││ │
│ │ └───────────────────────────────────────────┘ │ │
│ │ [Add] [Edit] [Remove] [↑] [↓]                │ │ ← Rule Management
│ └─────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────┤
│ Status: Watching 'C:\Downloads'    [Start][Stop]│ ← Control Bar
└─────────────────────────────────────────────────┘
```

### RuleStepsForm Layout
```
┌─────────────────────────────────────────────────┐
│ Rule: "Download Organizer"                      │
├─────────────────────────────────────────────────┤
│ Steps:                                          │
│ ┌───────────────────────────────────────────────┐ │
│ │ 1. ☑ Copy to Backup      → C:\Backup         │ │
│ │ 2. ☑ Rename with Date    → {date}_original   │ │
│ │ 3. ☑ Move to Processed   → C:\Processed      │ │
│ └───────────────────────────────────────────────┘ │
│ [Add Step] [Edit] [Remove] [↑] [↓]              │
├─────────────────────────────────────────────────┤
│                              [OK] [Cancel]      │
└─────────────────────────────────────────────────┘
```

### System Tray Integration
```
System Tray Context Menu:
├─ Open                    (Bold, default)
├─ ─────────────────────
├─ Start Watching
├─ Stop Watching (Downloads)  ← Shows folder name when active
├─ ─────────────────────
├─ Settings...
├─ Exit
```

## File Processing Workflow

### 1. File Detection
```csharp
// FileSystemWatcher events
private void OnFileCreated(object? sender, FileSystemEventArgs e)
{
    // Async processing to avoid blocking UI
    _ = Task.Run(() => ProcessFileAsync(e.FullPath));
}

private void OnFileRenamed(object? sender, RenamedEventArgs e)
{
    // Handle file renames (often indicates completion of downloads)
    _ = Task.Run(() => ProcessFileAsync(e.FullPath));
}
```

### 2. File Readiness Check
```csharp
private async Task ProcessFileAsync(string filePath)
{
    // Check if file is ready for processing
    var readiness = FileAccessibilityChecker.CheckFileReadiness(filePath, LogLive);
    
    if (!readiness.IsReady)
    {
        if (readiness.Reason == FileNotReadyReason.TemporaryFile)
        {
            LogLive($"Skipping temporary file: {Path.GetFileName(filePath)}");
            return;
        }
        
        // Wait for file to become accessible
        var accessResult = await FileAccessibilityChecker.WaitForFileAccessAsync(filePath);
        if (!accessResult.IsAccessible)
        {
            LogLive($"File remained inaccessible: {accessResult.Message}", isError: true);
            return;
        }
    }
    
    // Process with rules
    await ProcessWithRules(filePath);
}
```

### 3. Rule Matching and Execution
```csharp
private async Task ProcessWithRules(string filePath)
{
    var fileName = Path.GetFileName(filePath);
    
    _ruleManager.ExecuteWithReadLock(rules =>
    {
        foreach (var rule in rules.Where(r => r?.Enabled == true))
        {
            if (IsMatch(rule.Pattern, fileName))
            {
                if (rule.Steps?.Any() == true)
                {
                    // Multi-step processing
                    ExecuteRuleSteps(rule, filePath);
                }
                else
                {
                    // Single action
                    ApplyRule(rule, filePath);
                }
                return; // Stop after first match
            }
        }
    });
}
```

## Pattern Matching System

### Supported Patterns
- **Wildcards**: `*.txt`, `photo_*.jpg`, `document_???.pdf`
- **Extensions**: `*.pdf`, `*.{jpg,png,gif}`
- **Prefixes/Suffixes**: `temp_*`, `*_backup`
- **Exact Matches**: `important.docx`

### Pattern Implementation
```csharp
private bool IsMatch(string pattern, string fileName)
{
    // Convert glob pattern to regex
    var regexPattern = "^" + Regex.Escape(pattern)
        .Replace(@"\*", ".*")
        .Replace(@"\?", ".") + "$";
    
    return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
}
```

## Error Handling Strategy

### Graceful Degradation
- **Rule Loading Errors**: Continue with empty rule set, log error
- **File Access Errors**: Retry with exponential backoff, then skip
- **Settings Errors**: Use default settings, attempt to recreate
- **UI Errors**: Log error, continue operation

### Logging System
```csharp
// Dual logging approach
private void LogRuleAction(string message)
{
    // Historical log (always enabled)
    ruleLogTextBox.AppendText($"[{DateTime.Now:G}] {message}\n");
}

private void LogLive(string message, bool isError = false)
{
    // Live log (can be disabled)
    if (!_liveLogEnabled) return;
    
    liveLogTextBox.SelectionColor = isError ? Color.Red : Color.Black;
    liveLogTextBox.AppendText($"[{DateTime.Now:G}] {message}\n");
}
```

## WPF Migration Recommendations

### UI Framework Mapping

**Current Windows Forms → Recommended WPF/MahApps.Metro**:

1. **MainForm TabControl** → `MahApps.Metro.Controls.MetroTabControl`
   - Material Design tabs → Metro-styled tabs
   - Maintains tabbed interface for Rules/Log/LiveLog

2. **ListView (Rules)** → `DataGrid` with MVVM binding
   - Better data binding support
   - Built-in sorting, filtering, grouping
   - Enhanced column management

3. **RichTextBox (Logs)** → `FlowDocumentScrollViewer` or `TextBox`
   - Better text formatting capabilities
   - Improved scrolling and search

4. **System Tray** → `System.Windows.Forms.NotifyIcon` (unchanged)
   - WPF doesn't have native tray support
   - Continue using Windows Forms NotifyIcon

5. **Dialogs** → `MahApps.Metro.Controls.Dialogs`
   - Modern metro-styled dialogs
   - Better theming integration

### Architecture Improvements for WPF

**MVVM Pattern Implementation**:
```csharp
// ViewModels
public class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<RuleViewModel> Rules { get; set; }
    public ICommand StartWatchingCommand { get; set; }
    public ICommand StopWatchingCommand { get; set; }
    public ICommand AddRuleCommand { get; set; }
}

public class RuleViewModel : INotifyPropertyChanged
{
    public Rule Model { get; set; }
    public ICommand EditCommand { get; set; }
    public ICommand DeleteCommand { get; set; }
}

// Services (keep existing)
public interface IRuleService
{
    Task<IEnumerable<Rule>> GetRulesAsync();
    Task SaveRuleAsync(Rule rule);
    Task DeleteRuleAsync(Rule rule);
}
```

**Data Binding Benefits**:
- **Automatic UI Updates**: ObservableCollection automatically updates ListView
- **Command Pattern**: Better separation of UI and logic
- **Validation Support**: Built-in data validation framework
- **Testability**: ViewModels can be unit tested without UI

### Theme Integration

**MahApps.Metro Theming**:
```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml" />
            <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml" />
            <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

**Theme Manager Replacement**:
```csharp
// Replace current ThemeManager with MahApps theming
ThemeManager.Current.ChangeTheme(Application.Current, "Light.Blue");
ThemeManager.Current.ChangeTheme(Application.Current, "Dark.Blue");
```

### Migration Strategy

**Phase 1: Core Architecture**
1. Create WPF project with MahApps.Metro
2. Implement MVVM structure with ViewModels
3. Create services interfaces for rule management
4. Port core business logic (Rule, RuleStep, FileAccessibilityChecker)

**Phase 2: Main Interface**
1. Design MainWindow with MetroWindow base
2. Implement tabbed interface with MetroTabControl
3. Create rule management with DataGrid
4. Port logging systems with better WPF text controls

**Phase 3: Dialogs and Forms**
1. Convert RuleStepsForm to WPF UserControl
2. Implement SettingsDialog with MahApps styling
3. Port HelpViewer to WPF with better text rendering

**Phase 4: System Integration**
1. Maintain Windows Forms NotifyIcon for tray
2. Implement WPF-friendly file system monitoring
3. Add WPF-specific features (animations, better theming)

### Benefits of WPF Migration

**Technical Advantages**:
- **Better Data Binding**: Automatic UI updates, reduced boilerplate
- **Improved Theming**: Consistent dark/light mode support
- **Enhanced Graphics**: Vector graphics, better DPI scaling
- **Modern UI Patterns**: MVVM, dependency injection, commanding

**User Experience Improvements**:
- **Responsive UI**: Better threading model, smoother interactions
- **Accessibility**: Enhanced screen reader support
- **Touch Support**: Better touch and high-DPI display handling
- **Modern Look**: Metro design language, consistent with Windows 11

**Development Benefits**:
- **Testability**: MVVM pattern enables better unit testing
- **Maintainability**: Clear separation of concerns
- **Extensibility**: Easier to add new features and UI components
- **Community**: Larger ecosystem of WPF/MVVM libraries and tools

## Testing Strategy

### Current Test Coverage
- **FileHandlingTests.cs**: Comprehensive tests for file accessibility and retry logic
- **Manual Testing**: UI interactions and rule processing
- **Integration Testing**: End-to-end file processing workflows

### Recommended Test Expansion
```csharp
// Unit Tests
[TestClass]
public class RuleManagerTests
{
    [TestMethod]
    public async Task LoadRulesAsync_ShouldHandleCorruptedFile()
    
    [TestMethod]
    public void ExecuteWithReadLock_ShouldAllowConcurrentReads()
}

[TestClass]
public class RuleMatchingTests
{
    [TestMethod]
    public void IsMatch_ShouldMatchWildcards()
    
    [TestMethod]
    public void IsMatch_ShouldBeCaseInsensitive()
}

// Integration Tests
[TestClass]
public class FileProcessingIntegrationTests
{
    [TestMethod]
    public async Task ProcessFileAsync_ShouldApplyMatchingRule()
    
    [TestMethod]
    public async Task ProcessFileAsync_ShouldSkipTemporaryFiles()
}
```

## Deployment Considerations

### Current Deployment
- **Self-contained executable**: Single-file deployment
- **Settings persistence**: JSON files in application directory
- **Windows startup integration**: Registry-based startup

### Recommended Improvements
- **ClickOnce deployment**: Automatic updates
- **MSI installer**: Professional installation experience
- **Windows Store**: Modern app distribution
- **Containerization**: Docker support for server scenarios

## Performance Characteristics

### Current Performance
- **File Detection**: Sub-second response to file system events
- **Rule Processing**: Millisecond-level pattern matching
- **UI Responsiveness**: Async processing prevents UI blocking
- **Memory Usage**: Minimal footprint with efficient rule storage

### Optimization Opportunities
- **Batch Processing**: Process multiple files simultaneously
- **Smart Monitoring**: Pause monitoring during bulk operations
- **Caching**: Cache compiled patterns for better performance
- **Resource Management**: Better disposal of file handles

This comprehensive documentation provides everything needed to understand the current FolderWatcher application architecture and successfully migrate it to WPF with MahApps.Metro while preserving all existing functionality and improving the user experience.
