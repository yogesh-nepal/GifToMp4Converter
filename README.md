# GifToMp4Converter

A simple .NET console application that batch-converts GIFs to MP4 videos using FFmpeg (via the Xabe.FFmpeg library). It ensures each output is at least 4 seconds long by padding shorter GIFs with black frames, encodes with H.264, and embeds optional metadata.

---

## Features

- **Batch conversion**: Processes all `.gif` files in a specified input folder.  
- **Duration padding**: GIFs shorter than 4 seconds are padded with black frames to reach a minimum length of 4 seconds.  
- **H.264 encoding**: Uses the widely supported H.264 codec for MP4 output.  
- **Sequential naming**: Outputs are named `1.mp4`, `2.mp4`, etc.  
- **Metadata tags**: Embeds title, artist, album, genre, comment, date, copyright, description, encoder, language, and rating.  
- **Error resilience**: Continues processing even if individual files fail.

---

## Prerequisites

1. **.NET 6 or later** installed on your machine (tested with .NET 8).  
2. **FFmpeg** binaries (Windows 64-bit GPL shared build recommended).  
3. **Xabe.FFmpeg** NuGet package (installed via the project file).

---

## Installation

1. **Clone the repository**  
   ```bash
   git clone https://github.com/yourusername/GifToMp4Converter.git
   cd GifToMp4Converter
