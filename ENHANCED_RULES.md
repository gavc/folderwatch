# Enhanced Rule System - FolderWatch WPF

## Overview

The FolderWatch application now includes a comprehensive enhanced rule system that supports multi-action workflows, pattern-based renaming, and advanced file processing capabilities.

## Key Features

### 1. Multi-Action Support

- **Move**: Move files to different locations
- **Copy**: Copy files while keeping originals
- **Delete**: Delete files with safety checks
- **Rename**: Rename files using advanced patterns

### 2. Multi-Step Rules

- Create rules with multiple sequential actions
- Each rule can have unlimited steps that execute in order
- Steps can be enabled/disabled individually
- Output of one step becomes input for the next

### 3. Pattern-Based Renaming

The rename functionality now supports powerful variable substitution:

#### Available Variables

- `{filename}` - Original filename without extension
- `{extension}` - File extension without dot
- `{fullextension}` - File extension with dot
- `{datetime}` - Current date and time (default format: yyyyMMdd_HHmmss)
- `{datetime:format}` - Custom date/time format (e.g., {datetime:yyyy-MM-dd})
- `{date}` - Current date only (default: yyyy-MM-dd)
- `{time}` - Current time only (default: HH-mm-ss)
- `{counter}` - Sequential number (default: 000)
- `{counter:format}` - Custom counter format (e.g., {counter:0000})
- `{guid}` - Short unique identifier
- `{guidfull}` - Full unique identifier

#### Example Patterns

- `{filename}_backup` → `document_backup.txt`
- `processed_{datetime:yyyyMMdd}_{filename}` → `processed_20241203_document.txt`
- `archive_{counter:000}_{filename}` → `archive_001_document.txt`
- `{filename}_{guid}` → `document_a1b2c3d4.txt`

### 4. File Pattern Matching

Enhanced pattern matching supports:

- Basic wildcards: `*.txt`, `photo_*.jpg`
- Multiple extensions: `*.{jpg,png,gif}`
- Complex patterns: `document_*_final.pdf`

### 5. Safety Features

#### Delete Operation Safety

- Skip system and hidden files (configurable)
- File size limits (default: 1GB max)
- Configurable safety settings
- Detailed logging of skipped operations

#### File Conflict Resolution

- Automatic unique filename generation for conflicts
- Prevents overwriting existing files
- Incremental numbering (e.g., `file_001.txt`, `file_002.txt`)

### 6. Rule Testing

- Test rules against sample filenames before applying
- Preview what actions would be performed
- Validation of rule configuration
- Warning for destructive operations

## Using the Enhanced Rule System

### Creating Rules

1. Click "Add Rule" in the main interface
2. Enter a rule name and file pattern
3. Add action steps:
   - Choose action type (Copy, Move, Rename, Delete)
   - Configure action-specific settings
   - Use the pattern helper for rename operations

### Rule Editor Features

- **Multi-step workflow**: Add multiple sequential actions
- **Step management**: Reorder, enable/disable, or delete steps
- **Pattern variables**: Use variable substitution in rename patterns
- **Validation**: Real-time validation with error feedback
- **Browse dialogs**: Easy folder selection for destinations

### Testing Rules

1. Select a rule from the list
2. Click "Test Rule"
3. Enter a sample filename
4. Review the preview of actions that would be performed

### Rule Execution

Rules are executed in order of priority:
1. First matching enabled rule is processed
2. Multi-step rules execute all enabled steps sequentially
3. Detailed logging of each action
4. Error handling prevents corruption of workflow

## Configuration

### Safety Settings

Configure delete operation safety in application settings:

- `SkipSystemFiles`: Skip files with system attributes
- `SkipHiddenFiles`: Skip files with hidden attributes
- `MaxFileSizeBytes`: Maximum file size for deletion
- `RequireConfirmation`: Prompt before deleting
- `UseRecycleBin`: Move to recycle bin instead of permanent deletion

### File Processing

- `MaxRetries`: Number of retry attempts for locked files
- `InitialDelayMs`: Initial retry delay
- `BackoffMultiplier`: Exponential backoff multiplier
- `SkipTemporaryFiles`: Skip temporary file extensions

## Best Practices

1. **Test First**: Always test rules before enabling them
2. **Start Simple**: Begin with single-action rules before creating complex workflows
3. **Use Patterns**: Leverage variable patterns for consistent naming
4. **Safety First**: Configure appropriate safety settings for delete operations
5. **Monitor Logs**: Review rule execution logs to ensure proper operation

## Troubleshooting

### Common Issues

1. **Rule Not Matching**: Check file pattern syntax and test with sample filenames
2. **Step Validation Errors**: Ensure all required fields are filled for each step
3. **File Access Errors**: Check file permissions and retry settings
4. **Pattern Errors**: Verify variable syntax and use pattern examples

### Debugging

- Use the rule testing feature to validate patterns
- Check the Rule Log tab for detailed execution information
- Review validation errors in the rule editor
- Monitor the Live Log for real-time file processing events

## Technical Details

### Architecture

- **MVVM Pattern**: Clean separation of UI and business logic
- **Async Operations**: Non-blocking file operations
- **Thread Safety**: Safe concurrent access to rules and settings
- **Error Handling**: Comprehensive exception handling and logging

### Performance

- **Sequential Processing**: Rules processed one at a time per file
- **File Locking**: Proper handling of locked and temporary files
- **Memory Management**: Efficient resource usage and cleanup
- **Logging Limits**: Automatic log size management

## Migration from Simple Rules

Existing simple rules are automatically compatible with the enhanced system:
- Single-action rules continue to work as before
- Can be edited to add additional steps
- All existing functionality is preserved