# SemantiClip

SemantiClip is a powerful application that processes videos by downloading them from YouTube, transcribing the audio, generating chapters, and creating blog posts based on the content. It's built with .NET and Blazor WebAssembly, providing a modern and responsive user interface.

## Table of Contents
- [SemantiClip](#semanticlip)
  - [Table of Contents](#table-of-contents)
  - [About The Project](#about-the-project)
  - [Built With](#built-with)
  - [Getting Started](#getting-started)
    - [Prerequisites](#prerequisites)
    - [Installation](#installation)
  - [Usage](#usage)
  - [Roadmap](#roadmap)
  - [Contributing](#contributing)
  - [License](#license)
  - [Contact](#contact)

## About The Project

SemanticClip is designed to help content creators and educators by automating the process of creating written content from video material. The application:

- Downloads videos from YouTube
- Extracts audio using FFmpeg
- Transcribes audio content using Azure OpenAI Whisper
- Generates chapter markers using GPT-4o for better content organization
- Creates blog posts based on the transcript and chapters
- Provides a modern web interface for easy interaction

## Built With

* [.NET 9](https://dotnet.microsoft.com/)
* [Blazor WebAssembly](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
* [MudBlazor](https://mudblazor.com/) - UI Component Library
* [Azure OpenAI](https://azure.microsoft.com/en-us/products/cognitive-services/openai-service)
* [FFmpeg](https://ffmpeg.org/) - Media processing library

## Getting Started

### Prerequisites

* .NET 9 SDK
* Azure account with OpenAI service deployed
* FFmpeg installed on the server
* YouTube API key (for video downloading)

### Installation

1. Clone the repo
   ```bash
   git clone https://github.com/your_username/SemantiClip.git
   ```

2. Configure Azure OpenAI Services
   - Set up Azure OpenAI service
   - Deploy Whisper model for transcription (recommended: whisper)
   - Deploy GPT-4o model for content generation (recommended: gpt-4o)
   - Add your API keys and deployment names to the configuration

3. Install FFmpeg
   ```bash
   # macOS
   brew install ffmpeg
   
   # Ubuntu
   sudo apt-get install ffmpeg
   
   # Windows
   # Download from https://ffmpeg.org/download.html and add to PATH
   ```

4. Configure YouTube API
   - Create a YouTube API key
   - Add the key to your configuration

4. Run the application
   ```bash
   cd SemanticClip.API
   dotnet run
   
   # In a new terminal
   cd SemanticClip.Client
   dotnet run
   ```

## Usage

1. Open the application in your browser (default: http://localhost:5186)
2. Enter a YouTube URL or upload a video file
3. Click "Process Video"
4. Wait for the processing to complete
5. View the generated transcript, chapters, and blog post

## Roadmap

- [x] Improve transcription quality with Whisper
- [x] Implement FFmpeg for better audio extraction
- [x] Use specialized models for different tasks
- [ ] Add support for multiple video formats
- [ ] Implement batch processing
- [ ] Add export options (PDF, Word, etc.)
- [ ] Improve chapter generation accuracy
- [ ] Add support for multiple languages
- [ ] Implement user authentication
- [ ] Add video editing capabilities

## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

Distributed under the MIT License. See `LICENSE` for more information.

## Contact

Your Name - [@your_twitter](https://twitter.com/your_twitter) - email@example.com

Project Link: [https://github.com/vicperdana/SemantiClip](https://github.com/vicperdana/SemantiClip)
