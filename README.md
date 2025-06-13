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

## Deploy to Azure Container Apps

SemantiClip can be easily deployed to Azure Container Apps using the Azure Developer CLI (azd). This provides a scalable, serverless container hosting solution with built-in monitoring and secrets management.

### Prerequisites for Azure Deployment

* Azure subscription
* [Azure Developer CLI (azd)](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd) installed
* [Docker](https://docs.docker.com/get-docker/) installed (for local testing)

### Quick Deploy

1. **Clone and navigate to the repository**
   ```bash
   git clone https://github.com/vicperdana/SemantiClip.git
   cd SemantiClip
   ```

2. **Initialize azd environment**
   ```bash
   azd init
   # When prompted, select "semanticlip" as the template
   ```

3. **Login to Azure**
   ```bash
   azd auth login
   ```

4. **Deploy to Azure**
   ```bash
   azd up
   ```
   
   This single command will:
   - Provision all required Azure resources (Container Apps, Key Vault, Log Analytics, etc.)
   - Build and push the container image
   - Deploy the application
   - Configure environment variables and secrets

### Configure Application Settings

After deployment, you'll need to configure the application secrets in Azure Key Vault:

1. **Set Azure OpenAI configuration**
   ```bash
   azd env set AzureOpenAI__Endpoint "https://your-openai-service.openai.azure.com/"
   azd env set AzureOpenAI__WhisperDeploymentName "whisper"
   azd env set AzureOpenAI__ContentDeploymentName "gpt-4o"
   ```

2. **Add secrets to Key Vault** (via Azure portal or CLI)
   - `AzureOpenAIKey`: Your Azure OpenAI API key
   - `GitHubPersonalAccessToken`: GitHub PAT for blog publishing
   - `AzureAIAgentConnectionString`: Azure AI Agent connection string

3. **Update deployment with new settings**
   ```bash
   azd deploy
   ```

### Monitoring and Logs

- **Application Insights**: Monitor application performance and issues
- **Log Analytics**: View detailed application logs
- **Azure Portal**: Access via the Container Apps resource

### Custom Configuration

You can customize the deployment by modifying:
- `azure.yaml`: Service configuration and hooks
- `infra/main.bicep`: Infrastructure as code (Bicep templates)
- `src/SemanticClip.API/Dockerfile`: Container configuration

### GitHub Actions CI/CD

The repository includes a GitHub Actions workflow (`.github/workflows/azure-dev.yml`) for automated deployment:

1. **Set up GitHub secrets**:
   - `AZURE_CREDENTIALS`: Service principal credentials
   - Or configure federated identity with `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`

2. **Set up GitHub variables**:
   - `AZURE_ENV_NAME`: Your environment name
   - `AZURE_LOCATION`: Azure region (e.g., "eastus")
   - `AZURE_SUBSCRIPTION_ID`: Your Azure subscription ID

3. **Automatic deployment**: Push to main branch triggers deployment

### Scaling and Performance

Azure Container Apps automatically scales based on:
- HTTP requests (configured for 50 concurrent requests per replica)
- CPU and memory usage
- Custom scaling rules (can be added via Bicep templates)

**Resource allocation per replica:**
- CPU: 0.5 cores
- Memory: 1.0 GB
- Min replicas: 1
- Max replicas: 10

### Troubleshooting

1. **Check deployment logs**
   ```bash
   azd logs
   ```

2. **Verify environment variables**
   ```bash
   azd env get-values
   ```

3. **Access container logs**
   ```bash
   az containerapp logs show \
     --name <container-app-name> \
     --resource-group <resource-group-name>
   ```

4. **Common issues**:
   - Ensure all required secrets are set in Key Vault
   - Verify Azure OpenAI service deployment names match configuration
   - Check that the container registry has the latest image

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
