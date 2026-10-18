# RagnaController — Root

Root-level application files: entry point, application definition, and global configuration.

## Files

| File | Responsibility |
|------|----------------|
| `App.xaml` | WPF application definition, resource dictionary, startup URI |
| `App.xaml.cs` | Entry point, DI container setup, global exception handling, application lifecycle |

## Notes

- `App.xaml.cs` configures the dependency injection container and registers all services
- Global unhandled exception handler logs crashes and shows user-friendly error dialog
- This folder should remain minimal — all logic lives in subfolders
