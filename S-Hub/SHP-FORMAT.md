# SHP Format Specification (.shp)

> **MosaicShell Setup Package Format (`.shp`)**
> The `.shp` format packages a complete, reproducible Windows desktop environment—including Rainmeter layout, MosaicShell tiles, wallpapers, application themes, and visual styles.

---

## 1. Overview & File Format

An `.shp` file is a standard **ZIP archive** renamed with the `.shp` extension, containing:
* `SHP-data.json` manifest at root.
* Component directories containing skins, themes, configurations, and wallpapers.

---

## 2. Filename Convention & Tag Encoding

Package filenames follow this pattern:
```text
<SetupName>{<Tags>}.shp
```

Example: `NordicMinimal{01RSW}.shp`

### Tag to ID Mapping Table

| Component | Tag Name | Single-Char ID | Description |
|---|---|---|---|
| Tessera | `Tessera` | `0` | Tessera flyouts & media widget |
| Mixdeck | `Mixdeck` | `1` | Mixdeck volume mixer |
| Inlay | `Inlay` | `2` | Inlay dock / application bar |
| Rainmeter | `Rainmeter` | `R` | General Rainmeter skins & layout |
| Firefox | `Firefox` | `F` | Firefox `userChrome.css` & custom CSS |
| Spicetify | `Spicetify` | `S` | Spicetify Spotify theme & color scheme |
| BetterDiscord | `BetterDiscord` | `D` | BetterDiscord active themes |
| Droptop | `Droptop` | `T` | Droptop dropdown bar settings |
| Windows VS | `WinVS` | `W` | Windows visual style (`.theme` & `.msstyles`) |

---

## 3. Directory Layout

Inside the `.shp` archive:

```text
├── SHP-data.json              # Setup metadata & component manifests
├── Wallpaper/
│   └── Wallpaper.<ext>        # Current desktop wallpaper (png, jpg, etc.)
├── Rainmeter/
│   ├── Rainmeter.ini          # Active skins and layout coordinates
│   ├── Skins/                 # Bundled third-party Rainmeter skin folders
│   ├── Plugins/               # Required 32-bit / 64-bit plugin DLLs
│   └── MosaicShell/           # Exported MosaicShell module variable files
├── AppSkins/
│   ├── Spicetify/
│   │   ├── Themes/            # Custom Spicetify theme assets
│   │   └── Extensions/        # Active Spicetify extension scripts
│   ├── BetterDiscord/         # Active BetterDiscord .theme.css files
│   └── Firefox/               # userChrome.css and chrome assets
└── WinVS/
    ├── <ThemeName>.theme      # Windows theme configuration file
    └── <MSStylesFolder>/      # Windows visual styles & shell directory
```

---

## 4. `SHP-data.json` Manifest Schema

The root manifest contains metadata, display geometry, and component-specific settings:

```json
{
  "Data": {
    "SetupName": "NordicMinimal",
    "ScreenSizeW": 1920,
    "ScreenSizeH": 1080,
    "WinBuild": 22631,
    "WinVer": "11",
    "WinScale": 100,
    "CoreModules": "Tessera|Mixdeck",
    "DLCs": [],
    "WinVS": "C:\\Windows\\Resources\\Themes\\Nordic.theme"
  },
  "Rainmeter": {
    "Skins": "Tessera|Mixdeck|ModularClocks",
    "Droptop": false
  },
  "Spicetify": {
    "current_theme": "Dribbblish",
    "color_scheme": "nord-dark",
    "extensions": "fullAppDisplay.js|popupLyrics.js"
  },
  "BetterDiscord": {
    "themelist": [
      "Nord.theme.css"
    ]
  },
  "Firefox": {},
  "Tags": [
    "Tessera",
    "Mixdeck",
    "Rainmeter",
    "Spicetify",
    "WinVS"
  ]
}
```

---

## 5. Tooling

* **Packager:** `S-Hub/shp-packager.ps1` — Reads current system layout, active themes, and compiles the `.shp`.
* **Extractor:** `S-Hub/shp-extractor.ps1` — Extracts `.shp`, verifies dependencies, and restores desktop layout.
