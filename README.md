<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge&logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/WPF-Desktop-blue?style=for-the-badge&logo=windows" alt="WPF" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="License" />
  <img src="https://img.shields.io/github/v/release/Szdunek/MP3-MP4-PDF-tool?style=for-the-badge&color=teal" alt="Release" />
</p>

<h1 align="center">Converter Splitter</h1>

<p align="center">
  <strong>All-in-one local file converter &amp; splitter tool</strong><br/>
  Video, Audio, PDF, Images &mdash; everything processed locally on your machine.
</p>

<p align="center">
  <em>&copy; DogSoft</em>
</p>

---

## Features

### Video &rarr; MP3
Convert video files to MP3 audio with selectable bitrate.

| Supported input formats |
|---|
| MP4, AVI, MKV, MOV, WMV, FLV, WebM, M4V, MPEG, MPG, 3GP |

- Batch conversion (multiple files at once)
- Bitrate selection: 128 / 192 / 256 / 320 kbps
- Progress tracking per file and overall
- Drag & drop support

---

### Audio Cutter
Cut and trim audio files with a visual waveform editor.

- **Waveform display** with real-time rendering
- **Selection handles** &mdash; drag start/end markers on the waveform
- **Playback controls** &mdash; play, pause, stop, play selection
- **Precise input** &mdash; manual start/end time in seconds
- Volume control
- Supports MP3, WAV, FLAC, OGG, AAC, WMA, M4A, OPUS

---

### Audio Converter
Convert between audio formats with full control over quality settings.

| From | To |
|---|---|
| MP3, WAV, FLAC, OGG, AAC, WMA, M4A, OPUS | MP3, WAV, FLAC, OGG, AAC |

- Bitrate selection: 96 / 128 / 192 / 256 / 320 kbps
- Sample rate: 22050 / 44100 / 48000 / 96000 Hz
- Batch conversion with progress tracking

---

### PDF Merge
Combine multiple PDF files into a single document.

- Drag & drop to add files
- **Reorder** files with up/down buttons
- Page count preview per file
- Total page count display

---

### PDF Split
Split a PDF into individual pages or custom ranges.

- **Split into individual pages** &mdash; each page becomes a separate file
- **Custom ranges** &mdash; e.g. `1-3, 4-6, 7-10`
- **Extract pages** &mdash; pick specific pages to export

---

### Image Converter & Resizer
Convert and resize images between popular formats.

| Supported formats |
|---|
| PNG, JPEG, BMP, GIF, WebP, TIFF |

- Quality slider (for JPEG/WebP)
- Optional resize with custom width/height
- Keep aspect ratio option
- Batch processing

---

## UI

- **Dark theme** with Material Design (purple & teal accents)
- **Sidebar navigation** with categorized sections (Media / Documents / Images)
- **Drag & drop** on every tab
- **Progress bars** for all conversion operations
- **Auto-update notifications** in sidebar

---

## Installation

### Option 1: Download release (recommended)
1. Go to [Releases](https://github.com/Szdunek/MP3-MP4-PDF-tool/releases)
2. Download `ConverterSplitter-X.X.X-win-x64-self-contained.zip`
3. Extract and run `ConverterSplitter.exe`

> The self-contained build includes .NET runtime &mdash; no additional installation needed.

### Option 2: Build from source
```bash
git clone https://github.com/Szdunek/MP3-MP4-PDF-tool.git
cd MP3-MP4-PDF-tool/ConverterSplitter
dotnet run
```

**Requirements for building:**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## FFmpeg

FFmpeg is required for video and audio processing (conversion, cutting).

- On first launch, if FFmpeg is not detected, the app will offer to **download it automatically** (~80 MB)
- FFmpeg is stored locally in the app directory
- Alternatively, install FFmpeg manually and add it to your system PATH

---

## Auto-Update

The app checks GitHub releases on startup. When a new version is available:
- A **green banner** appears in the sidebar
- Click it to see version details
- **Download & Install** updates automatically (downloads, replaces files, restarts)
- Or click **View on GitHub** to see the release page

---

## Releasing a new version

Push a version tag to trigger the CI/CD pipeline:

```bash
git tag 1.0.1
git push origin 1.0.1
```

The GitHub Actions workflow will:
1. Build self-contained and framework-dependent packages (win-x64)
2. Create ZIP archives
3. Publish a GitHub Release with both packages attached

---

## Tech Stack

| Component | Technology |
|---|---|
| Framework | .NET 8 / WPF |
| UI | [Material Design In XAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) |
| MVVM | [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) |
| Video/Audio | [FFmpeg](https://ffmpeg.org/) via process calls |
| Audio Playback | [NAudio](https://github.com/naudio/NAudio) |
| PDF | [PdfSharpCore](https://github.com/ststeiger/PdfSharpCore) |
| Images | [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp) |
| CI/CD | GitHub Actions |

---

## Project Structure

```
ConverterSplitter/
├── Controls/          # Custom WPF controls (WaveformControl)
├── Converters/        # XAML value converters
├── Resources/         # Styles and themes
├── Services/          # Business logic
│   ├── FFmpegService.cs       # FFmpeg process wrapper
│   ├── FFmpegDownloader.cs    # Auto-download FFmpeg
│   ├── PdfService.cs          # PDF merge/split operations
│   ├── ImageService.cs        # Image conversion/resize
│   └── UpdateService.cs       # GitHub release checker
├── ViewModels/        # MVVM ViewModels
├── Views/             # WPF UserControls and Windows
├── App.xaml           # Application entry point
└── MainWindow.xaml    # Main window with sidebar navigation
```

---

<p align="center">
  <sub>Built with .NET 8 &bull; &copy; DogSoft</sub>
</p>
