# VideoDownloader

**A Modern Windows Desktop Application for Downloading Videos and Playlists**

VideoDownloader is a Windows desktop application built with **C# and WPF (.NET 9)** that provides a simple and user-friendly way to download online videos and playlists.
Powered by **yt-dlp**, the application allows users to analyze video information, select available video qualities, monitor download progress, and manage their download history through a clean desktop interface.

---

## Overview

VideoDownloader was developed to provide a convenient desktop experience for downloading videos without relying on complicated command-line operations.
The application combines the power of yt-dlp with a Windows Presentation Foundation (WPF) interface, making video downloading and management easier for everyday users.

## Features
### Video Downloading

- Download individual videos using their URLs.
- Analyze video information before downloading.
- Display video title, thumbnail, duration, and available formats.
- Select from available video resolutions and qualities.
- Download video and audio together.
- Merge video and audio into an MP4 output format.
- Choose a custom destination folder.

### Playlist Downloading

- Analyze video playlists.
- Display playlist videos.
- Select videos to download.
- Download selected playlist videos.
- Track download results for individual videos.

### Download Progress Monitoring

- Display real-time download progress.
- Monitor download speed.
- Display estimated remaining time.
- Track downloaded data and total download size.
- Support download cancellation.

### Download History

- Automatically save download records.
- Store history in a local JSON file.
- View previous downloads.
- Filter downloads by status:
  - Completed
  - Failed
  - Cancelled
- Search by video title, URL, or download source.
- Open the folder containing a downloaded video.
- Delete individual history records without deleting the actual video files.

### User Interface

- Windows desktop interface built with WPF.
- Organized windows for individual downloads, playlists, and download history.
- Simple and practical user experience.

---

## Technologies Used

| Technology     | Purpose                                      |
| -------------- | -------------------------------------------- |
| C#             | Main programming language                    |
| .NET 9         | Application framework                        |
| WPF            | Desktop user interface                       |
| XAML           | UI layout and design                         |
| YoutubeDLSharp | .NET wrapper for yt-dlp                      |
| yt-dlp         | Video information extraction and downloading |
| FFmpeg         | Audio/video processing and merging           |
| JSON           | Local download history storage               |

---

## Project Structure

```text
VideoDownloader/
│
├── Models/
│   ├── VideoInfo.cs
│   ├── VideoFormat.cs
│   ├── PlaylistVideo.cs
│   └── DownloadHistoryEntry.cs
│
├── Services/
│   ├── YtDlpService.cs
│   └── DownloadHistoryService.cs
│
├── Tools/
│   ├── yt-dlp.exe
│   └── ffmpeg.exe
│
├── DHJSON/
│   └── download-history.json
│
├── MainWindow.xaml
├── MainWindow.xaml.cs
│
├── PlaylistWindow.xaml
├── PlaylistWindow.xaml.cs
│
├── DownloadHistoryWindow.xaml
├── DownloadHistoryWindow.xaml.cs
│
└── VideoDownloader.csproj
```

*Note: The structure above represents the main application components. Actual files may vary depending on the current project version.*

---

## Requirements

Before running the project, make sure you have:

- Windows 10 or Windows 11.
- .NET 9 SDK.
- Visual Studio 2022 or a compatible .NET development environment.
- yt-dlp executable.
- FFmpeg executable.
- Deno JavaScript runtime for yt-dlp operations that require a JavaScript runtime.

## Installation & Setup

### 1. Clone the Repository

```bash
git clone https://github.com/YOUR_USERNAME/VideoDownloader.git
```

### 2. Open the Project

Open `VideoDownloader.sln` or `VideoDownloader.csproj` using Visual Studio.

### 3. Install Dependencies

Restore the NuGet packages:

```bash
dotnet restore
```

The project uses YoutubeDLSharp as its .NET integration library.

### 4. Configure Required Tools

Create a `Tools` directory inside the application's output directory and place the required executables there:

```text
Tools/
├── yt-dlp.exe
└── ffmpeg.exe
```

The application expects these tools at:

```csharp
Path.Combine(AppContext.BaseDirectory, "Tools", "yt-dlp.exe");
Path.Combine(AppContext.BaseDirectory, "Tools", "ffmpeg.exe");
```

Make sure the executable paths match your application configuration.

### 5. Build and Run

```bash
dotnet build
dotnet run
```

Alternatively, run the project directly from Visual Studio.

---

## Download History Storage

The application stores download history locally in JSON format.

Default development location:

```text
DHJSON/download-history.json
```

Each history record contains information such as:

- Video title.
- Video URL.
- Download status.
- Save directory.
- Download date.
- Download source.

This allows users to review and manage their previous downloads without requiring a database server.

---

## Roadmap

The project is continuously evolving. Planned improvements include:

- [ ] Unified download queue for individual videos and playlists.
- [ ] Sequential downloads with queue ordering.
- [ ] Individual progress tracking for each queued video.
- [ ] Download priority management.
- [ ] Remove waiting downloads from the queue.
- [ ] Retry failed downloads.
- [ ] Further UI and user experience improvements.
- [ ] Additional playlist management features.

---

## Project Goals

The main goals of VideoDownloader are:

- Build a practical Windows desktop application using C# and WPF.
- Learn and apply software development concepts through a real-world project.
- Practice working with external command-line tools and .NET libraries.
- Implement asynchronous operations and real-time progress reporting.
- Improve application architecture through Models and Services.
- Build a maintainable foundation for future features.

---

## Disclaimer

VideoDownloader is an independent software project that uses yt-dlp and FFmpeg.

Users are responsible for ensuring that their downloads comply with the applicable laws, copyright regulations, and the terms of service of the platforms they use.

This project is not affiliated with, endorsed by, or officially connected to YouTube or any other video-sharing platform.

---

## License

No license has been specified yet.

If you intend to make this project open source, consider adding an appropriate license file to the repository.

---

**Developed with C# and .NET 9**

*VideoDownloader — Simple, Practical, and Efficient Video Downloading.*
