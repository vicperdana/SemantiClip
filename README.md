# SemanticClip

SemanticClip is a powerful application that processes videos by downloading them from YouTube, transcribing the audio, generating chapters, and creating blog posts based on the content. It's built with .NET and Blazor WebAssembly, providing a modern and responsive user interface.

## Table of Contents
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
- Transcribes audio content using Azure Speech Services
- Generates chapter markers for better content organization
- Creates blog posts based on the transcript and chapters
- Provides a modern web interface for easy interaction

## Built With

* [.NET 9](https://dotnet.microsoft.com/)
* [Blazor WebAssembly](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
* [MudBlazor](https://mudblazor.com/) - UI Component Library
* [Azure OpenAI](https://azure.microsoft.com/en-us/products/cognitive-services/openai-service)
* [Azure Speech Services](https://azure.microsoft.com/en-us/products/cognitive-services/speech-services)

## Getting Started

### Prerequisites

* .NET 9 SDK
* Azure account with OpenAI and Speech Services access
* YouTube API key (for video downloading)

### Installation

1. Clone the repo
   ```bash
   git clone https://github.com/your_username/SemanticClip.git
   ```

2. Configure Azure Services
   - Set up Azure OpenAI service
   - Configure Azure Speech Services
   - Add your API keys to the configuration

3. Configure YouTube API
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

Project Link: [https://github.com/your_username/SemanticClip](https://github.com/your_username/SemanticClip)
