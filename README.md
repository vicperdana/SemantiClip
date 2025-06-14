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
  - [Azure Deployment](#azure-deployment)
    - [Prerequisites for Azure Deployment](#prerequisites-for-azure-deployment)
    - [Deployment Steps](#deployment-steps)
    - [Environment Configuration](#environment-configuration)
    - [Required Azure Services Configuration](#required-azure-services-configuration)
    - [Monitoring and Logs](#monitoring-and-logs)
    - [Scaling and Performance](#scaling-and-performance)
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

## Getting Started

### Prerequisites

* .NET 9 SDK
* Azure account with OpenAI service deployed
* FFmpeg installed on the server
* Ollama installed for local LLM processing
* GitHub account with personal access token (for blog post publishing)

### Installation

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

5. Configure Azure OpenAI Services
   - Set up Azure OpenAI service
   - Deploy Whisper model for transcription (recommended: whisper)
   - Deploy GPT-4o model for content generation (recommended: gpt-4o)
   - Add your API keys and deployment names to the configuration

6. Configure `appsettings.Development.json` under the SemanticClip.API project
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

7. Run the application
   ```bash
   cd SemanticClip.API
   dotnet run
   
   # In a new terminal
   cd SemanticClip.Client
   dotnet run
   ```

## Azure Deployment

SemantiClip can be easily deployed to Azure App Service using the Azure Developer CLI (azd) for a one-command deployment experience.

### Prerequisites for Azure Deployment

- [Azure Developer CLI (azd)](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- An Azure subscription

### Deployment Steps

1. **Clone and navigate to the repository**
   ```bash
   git clone https://github.com/vicperdana/SemantiClip.git
   cd SemantiClip
   ```

2. **Login to Azure**
   ```bash
   azd auth login
   ```

3. **Set required environment variables**
   
   Before deploying, you must configure the following required environment variables:
   
   ```bash
   # Azure OpenAI Configuration
   azd env set AZURE_OPENAI_ENDPOINT "https://your-openai-service.openai.azure.com/"
   azd env set AZURE_OPENAI_API_KEY "your-azure-openai-api-key"
   
   # Azure AI Agent Configuration
   azd env set AZURE_AI_AGENT_CONNECTION_STRING "your-azure-ai-agent-connection-string"
   
   # GitHub Integration (for blog post publishing)
   azd env set GITHUB_PERSONAL_ACCESS_TOKEN "your-github-personal-access-token"
   ```
   
   > **Important**: These variables are required for the application to function properly. The deployment will fail if any of these are missing.

   **How to get these values:**
   - **AZURE_OPENAI_ENDPOINT**: Found in your Azure OpenAI resource overview page (e.g., `https://your-service.openai.azure.com/`)
   - **AZURE_OPENAI_API_KEY**: Found in your Azure OpenAI resource's "Keys and Endpoint" section
   - **AZURE_AI_AGENT_CONNECTION_STRING**: From your Azure AI Foundry project's connection settings
   - **GITHUB_PERSONAL_ACCESS_TOKEN**: Generate from GitHub Settings > Developer settings > Personal access tokens

4. **Initialize the environment** (first time only)
   ```bash
   azd init
   ```

5. **Deploy to Azure**
   ```bash
   azd up
   ```

   This command will:
   - Provision Azure resources (Resource Group, App Service Plan, App Services, Application Insights)
   - Deploy both the API and web application
   - Configure environment variables with default values

### Environment Configuration

The deployment automatically configures the following default values:
- `azureAiAgentMaxEvaluations`: 3
- `azureAiAgentChatModelId`: gpt-4o
- `azureOpenAiContentDeploymentName`: gpt-4o
- `azureOpenAiWhisperDeploymentName`: whisper
- `fileUploadMaxRequestBodySizeInBytes`: 30000000
- `fileUploadAllowedExtensions`: .mp4,.avi,.mov,.wmv,.mkv

To customize these values, use:
```bash
azd env set <KEY> <VALUE>
azd up
```

### Required Azure Services Configuration

Before deployment, ensure you have the following Azure services set up:

1. **Azure OpenAI Service**
   - Create an Azure OpenAI resource in your Azure subscription
   - Deploy the required models:
     - `gpt-4o` for content generation
     - `whisper` for audio transcription
   - Note your service endpoint and API key for the environment variables

2. **Azure AI Agent (Optional)**
   - Set up an Azure AI Foundry project
   - Get the connection string from your project settings

3. **GitHub Personal Access Token (Optional)**
   - Generate a GitHub Personal Access Token with repository permissions
   - Required only if you want to publish blog posts directly to GitHub

The deployment process will automatically configure these settings in your App Service using the environment variables you provided.

### Monitoring and Logs

- **Application Insights**: Automatically configured for monitoring and logging
- **Azure Portal**: Access logs and metrics through the Azure portal
- **Live Logs**: Use `az webapp log tail` for real-time log monitoring

### Scaling and Performance

The deployment uses:
- **App Service Plan**: Basic (B1) tier - suitable for development/testing
- **Linux containers**: For optimal .NET performance
- **Application Insights**: For performance monitoring

For production workloads, consider upgrading to Standard or Premium tiers.

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
