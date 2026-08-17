# 🌐 RagnaController Community Hub

## Overview
The Community Hub is an in-app store that allows users to browse, search, and download curated RagnaController profiles from the community. It's a serverless solution that fetches a "Master Registry" JSON file from GitHub Gist.

## Features
- **Glassmorphism UI**: Beautiful "Obsidian & Gold" themed interface
- **Live Search**: Filter profiles by name, class, or author
- **One-Click Install**: Download and install profiles with a single click
- **Auto-Refresh**: Reload registry on demand
- **Error Handling**: Graceful fallback messages for connection issues

## Setup Instructions

### Step 1: Create a GitHub Gist
1. Go to https://gist.github.com/new
2. Create a new gist named `registry.json`
3. Paste the sample content from `/mnt/c/RagnaController/Community/registry.json`
4. Click "Keep changes" and copy the raw URL (e.g., `https://gist.githubusercontent.com/YOUR_USERNAME/YOUR_GIST_ID/raw/registry.json`)

### Step 2: Update the Registry URL
Edit `/mnt/c/RagnaController/src/RagnaController/CommunityBrowserWindow.xaml.cs` and update line 18:

```csharp
private const string RegistryUrl = "https://gist.githubusercontent.com/YOUR_USERNAME/YOUR_GIST_ID/raw/registry.json";
```

### Step 3: Build and Test
```bash
cd /mnt/c/RagnaController
dotnet build
```

### Step 4: Run the Application
```bash
dotnet run
```

Click the "🌐 Community" button in the toolbar to open the Community Hub.

## Registry JSON Schema

```json
{
  "$schema": "https://gist.githubusercontent.com/YOUR_USERNAME/YOUR_GIST_ID/raw/schema.json",
  "profiles": [
    {
      "name": "Profile Name",
      "class": "mage|archer|thief|acolyte|merchant|gunslinger|ninja",
      "author": "Author Name",
      "description": "Short description of the profile",
      "shareCode": "gist://raw.githubusercontent.com/YOUR_USERNAME/YOUR_GIST_ID/raw/registry.json?profile=profile-name"
    }
  ]
}
```

## Adding New Profiles

To add a new profile to the registry:

1. Create a new profile using the Profile Wizard or manually edit a JSON file
2. Add it to the `profiles` array in your registry.json
3. Generate a unique shareCode for each profile

Example:
```json
{
  "name": "Wizard - Ice Master",
  "class": "wizard",
  "author": "CommunityUser123",
  "description": "Ice-based spells with freeze mechanics. Perfect for crowd control.",
  "shareCode": "gist://raw.githubusercontent.com/YOUR_USERNAME/YOUR_GIST_ID/raw/registry.json?profile=wizard-ice"
}
```

## Architecture

The Community Hub follows the existing RagnaController architecture:

- **MVVM Pattern**: Uses WPF data binding for the profile list
- **HttpClient**: Fetches registry from GitHub Gist with 10s timeout
- **ProfileShareService**: Reuses existing download logic for profile installation
- **ProfileManager**: Handles profile storage and persistence

## Security Notes

- The registry is fetched over HTTPS (GitHub)
- Profile installation uses the existing `ProfileShareService` which validates JSON structure
- No sensitive data is transmitted
- Users can manually inspect the registry.json content before installing

## Troubleshooting

### "Could not connect to the Community Hub"
- Check your internet connection
- Verify the GitHub Gist URL is correct and public
- Ensure the gist contains valid JSON

### Profile not appearing in dropdown
- The profile name must be unique (no duplicates)
- Check that the profile was successfully downloaded and saved
- Restart the application if needed

## Future Enhancements

- [ ] Add profile ratings/reviews
- [ ] Implement category filtering (class, difficulty, etc.)
- [ ] Add "featured profiles" section
- [ ] Support for private registries (GitHub OAuth)
- [ ] Profile preview before install
