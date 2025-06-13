# SemantiClip

<p align="center">
  <img src="/SemanticClip.Client/wwwroot/images/SemantiClip-app.png" alt="SemantiClip Logo">
</p>

> **Note**: This is a proof of concept application and is not intended for production use. It demonstrates the integration of various AI technologies for video processing and content generation.

**NEW**: Added GitHub with ModelContextProtocol (MCP) Integration

**SemantiClip** is a powerful AI-driven tool that converts videos into structured content by transcribing audio and creating blog posts. Built with .NET, Semantic Kernel and Blazor WebAssembly, it delivers a fast, modern, and responsive user experience.


## Table of Contents
- [SemantiClip](#semanticlip)
  - [Table of Contents](#table-of-contents)
  - [About The Project](#about-the-project)
  - [Built With](#built-with)
  - [Getting Started](#getting-started)
    - [Prerequisites](#prerequisites)
    - [Installation](#installation)
      - [Option 1: Local Development](#option-1-local-development)
      - [Option 2: Deploy to Azure App Service using Azure Developer CLI (azd)](#option-2-deploy-to-azure-app-service-using-azure-developer-cli-azd)
  - [Usage](#usage)
  - [Roadmap](#roadmap)
  - [Contributing](#contributing)
  - [License](#license)
  - [Contact](#contact)

## About The Project

**SemantiClip** is an AI-powered tool that transforms video content into structured written formats. Designed for content creators and educators, it automates transcription and blog post creation—making it easier than ever to repurpose video content.

<p align="center">
  <img src="/docs/images/SemantiClip-Overview.png" alt="SemantiClip Overview">
</p>

### Key Features

- 🎙️ **Audio Extraction** – Uses FFmpeg to extract audio from video files.
- ✍️ **Transcription** – Converts speech to text using Azure OpenAI Whisper.
- 📝 **Blog Post Creation** – Automatically generates readable blog posts from transcripts.
- 💻 **Modern Web UI** – Built with .NET 9, Blazor WebAssembly, and MudBlazor.
- 🧩 **Local Content Generation** – Supports on-device LLM processing with Ollama.
- 🔍 **Semantic Kernel Integration** – Utilizes Semantic Kernel Process and Agent frameworks for enhanced context and orchestration.
- 📗 **GitHub with ModelContextProtocol Integration** – Publishes blog posts directly to GitHub repositories with ModelContextProtocol.

SemantiClip helps you do more with your video content—faster, smarter, and effortlessly.


## Built With

* [.NET 9](https://dotnet.microsoft.com/)
* [Semantic Kernel Process Framework](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/process/process-framework)
* [Semantic Kernel Agent Framework](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/?pivots=programming-language-csharp)
* [Blazor WebAssembly](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
* [MudBlazor](https://mudblazor.com/) - UI Component Library
* [Azure OpenAI](https://azure.microsoft.com/en-us/products/cognitive-services/openai-service)
* [FFmpeg](https://ffmpeg.org/) - Media processing library
* [Ollama](https://ollama.ai/) - Local LLM for content generation
* [ModelContextProtocol](https://github.com/microsoft/ModelContextProtocol) - ModelContextProtocol for publishing blog posts to GitHub
* [Azure App Service](https://azure.microsoft.com/en-us/products/app-service/) - Cloud hosting platform
* [Azure Developer CLI (azd)](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/overview) - Developer CLI for Azure deployment

## Getting Started

### Prerequisites

* .NET 9 SDK
* Azure account with OpenAI service deployed
* FFmpeg installed on the server
* Ollama installed for local LLM processing
* GitHub account with personal access token (for blog post publishing)

## Getting Started

### Prerequisites

* .NET 9 SDK
* Azure account with OpenAI service deployed
* FFmpeg installed on the server
* Ollama installed for local LLM processing
* GitHub account with personal access token (for blog post publishing)

### Installation

#### Option 1: Local Development

1. Clone the repo
   ```bash
   git clone https://github.com/vicperdana/SemantiClip.git
   ```

2. Install Ollama
   ```bash
   # macOS
   brew install ollama
   
   # Windows
   # Download from https://ollama.ai/download
   ```

3. Configure Ollama Model
   ```bash
   # Start the Ollama service
   ollama serve

   # In a separate shell Pull the phi4-mini model (search other models at [Ollama](https://ollama.com/search))
   ollama run phi4-mini
   ```

4. Configure GitHub Integration
   ```bash
   # Create a GitHub personal access token with repo access - see more details [here](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#creating-a-fine-grained-personal-access-token)
   # Copy the generated token and add it to your appsettings.json 
   "GitHub": {
    "PersonalAccessToken": "yourGitHubToken"
    },
   ```
   
5. Install FFmpeg
   ```bash
   # macOS
   brew install ffmpeg
   
   # Ubuntu
   sudo apt-get install ffmpeg
   
   # Windows
   # Download from https://ffmpeg.org/download.html and add to PATH
   ```

6. Configure Azure OpenAI Services
   - Set up Azure OpenAI service
   - Deploy Whisper model for transcription (recommended: whisper)
   - Deploy GPT-4o model for content generation (recommended: gpt-4o)
   - Add your API keys and deployment names to the configuration

7. Configure `appsettings.Development.json` under the SemanticClip.API project
   ```json
   {
     "AzureOpenAI": {
       "Endpoint": "your-azure-openai-endpoint",
       "ApiKey": "your-azure-openai-api-key",
     },
     "LocalSLM": {
       "ModelId": "phi4-mini",
       "Endpoint": "http://localhost:11434"
     },
     "AzureAIAgent": {
       "ConnectionString": "your-azure-ai-agent-connection-string",
       "ChatModelId": "gpt-4o",
       "VectorStoreId": "semanticclipproject",
       "MaxEvaluations": "3"
   }
   ```

8. Run the application
   ```bash
   cd SemanticClip.API
   dotnet run
   
   # In a new terminal
   cd SemanticClip.Client
   dotnet run
   ```

#### Option 2: Deploy to Azure App Service using Azure Developer CLI (azd)

**Prerequisites:**
- [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)
- Azure subscription with sufficient permissions to create resources
- Azure OpenAI service already deployed with Whisper and GPT-4o models

**Quick Deployment:**

1. Clone and navigate to the repository
   ```bash
   git clone https://github.com/vicperdana/SemantiClip.git
   cd SemantiClip
   ```

2. Initialize the Azure Developer CLI environment
   ```bash
   azd init
   ```

3. Set required environment variables
   ```bash
   # Azure OpenAI Configuration
   azd env set AZURE_OPENAI_ENDPOINT "https://your-service.openai.azure.com/"
   azd env set AZURE_OPENAI_API_KEY "your-api-key"
   azd env set AZURE_OPENAI_WHISPER_DEPLOYMENT_NAME "whisper"
   azd env set AZURE_OPENAI_CONTENT_DEPLOYMENT_NAME "gpt-4o"
   
   # Azure AI Agent Configuration (if using Azure AI Foundry)
   azd env set AZURE_AI_AGENT_CONNECTION_STRING "your-connection-string"
   azd env set AZURE_AI_AGENT_CHAT_MODEL_ID "gpt-4o"
   
   # GitHub Integration (optional)
   azd env set GITHUB_PERSONAL_ACCESS_TOKEN "your-github-token"
   ```

4. Deploy to Azure
   ```bash
   azd up
   ```

   This single command will:
   - Provision all required Azure resources (Resource Group, App Service Plan, App Service, Key Vault, Application Insights)
   - Build and deploy the SemantiClip API
   - Configure all environment variables and app settings

5. Access your deployed application
   The deployment will output the URL of your deployed API. The format will be:
   ```
   https://app-api-[unique-id].azurewebsites.net
   ```

**Managing Environment Variables:**

You can update environment variables anytime using:
```bash
azd env set VARIABLE_NAME "new-value"
azd deploy  # Re-deploy to apply changes
```

**Monitoring and Logs:**
- View logs and metrics in the Azure portal under Application Insights
- Access App Service logs via Azure portal or Azure CLI
- Monitor health and performance through the integrated monitoring dashboard

## Usage

<p align="left">
  <img src="/docs/images/SemantiClipUsage.gif" alt="SemantiClip Usage">
</p>

1. Open the application in your browser (default: http://localhost:5186)
2. Upload a video file
3. Click "Process Video"
4. Wait for the processing to complete
5. View the generated transcript and blog post
6. To publish the blog post to GitHub, edit the instructions with a repo that you have write access to and click "Submit with MCP"

## Roadmap

- [x] Improve transcription quality with Whisper
- [x] Implement FFmpeg for better audio extraction
- [x] Use specialized models for different tasks
- [x] Add support for multiple video formats
- [x] Add GitHub with ModelContextProtocol Integration
- [x] Add Azure App Service deployment with Azure Developer CLI (azd)
- [ ] Add export options (PDF, Word, etc.)
- [ ] Implement user authentication
- [ ] Run using dotnet aspire
- [ ] Add unit tests and integration tests
- [ ] Optimize performance for large video files
- [ ] Update documentation and examples

## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

Distributed under the GPLv3 License. See `LICENSE` for more information.

## Contact

Vic Perdana - [LinkedIn](https://www.linkedin.com/in/vperdana/) - [GitHub](https://github.com/vicperdana)

Project Link: [https://github.com/vicperdana/SemantiClip](https://github.com/vicperdana/SemantiClip)
