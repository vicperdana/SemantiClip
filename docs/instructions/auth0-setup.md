# Auth0 Integration Setup

This document outlines the Auth0 integration implemented in SemantiClip.

## Configuration

### Auth0 Dashboard Settings

Configure the following URLs in your Auth0 Dashboard:

**Application Type**: Single Page Application (SPA)

**Allowed Callback URLs**:
- `http://localhost:5244/authentication/login-callback` (development)
- `https://semanticlipweb.vicperdana.com/authentication/login-callback` (production)

**Allowed Logout URLs**:
- `http://localhost:5244/` (development)
- `https://semanticlipweb.vicperdana.com/` (production)

**Allowed Web Origins**:
- `http://localhost:5244` (development)
- `https://semanticlipweb.vicperdana.com` (production)

### Application Settings

The following configuration has been added to both API and Client projects:

**API Configuration** (`appsettings.json`):
```json
{
  "Auth0": {
    "Domain": "dev-l88cnzvqt7n6fjp8.us.auth0.com",
    "Audience": "https://dev-l88cnzvqt7n6fjp8.us.auth0.com/api/v2/"
  }
}
```

**Client Configuration** (`appsettings.json`):
```json
{
  "Auth0": {
    "Authority": "https://dev-l88cnzvqt7n6fjp8.us.auth0.com",
    "ClientId": "0cuewiLDgvrlNcfg8TB8R4kdTxqKAEGl",
    "Audience": "https://dev-l88cnzvqt7n6fjp8.us.auth0.com/api/v2/"
  }
}
```

## Features Implemented

### 1. JWT Bearer Authentication (API)
- Configured in `Program.cs` with proper token validation
- Controllers can be protected with `[Authorize]` attribute
- Currently disabled for testing purposes (commented out)

### 2. OIDC Authentication (Blazor WebAssembly)
- Auth0 SPA integration with PKCE flow
- Automatic token handling for API calls
- Login/logout functionality

### 3. Authentication Components
- **LoginDisplay**: Shows user info and login/logout buttons
- **Authentication**: Handles Auth0 redirects
- **Profile**: Protected page showing user claims
- **RedirectToLogin**: Redirects unauthenticated users

### 4. Navigation
- Profile link appears only for authenticated users
- Clean navigation menu with authentication state awareness

## Usage

### Testing Authentication
1. Navigate to `/profile` to trigger authentication
2. Click "Log in" in the top navigation
3. Complete Auth0 login flow
4. View user profile and claims

### Enabling API Protection
Uncomment the `[Authorize]` attributes in:
- `VideoProcessingController.cs`
- `BlogPublishingController.cs`

### API Access Tokens
The Blazor client automatically includes access tokens when calling the API. The `BaseAddressAuthorizationMessageHandler` handles token attachment.

## Architecture

```
Blazor WebAssembly Client
├── Auth0 SPA (OIDC + PKCE)
├── Automatic token management
└── API calls with Bearer tokens

ASP.NET Core API
├── JWT Bearer validation
├── Auth0 audience verification
└── Protected endpoints
```

## Security Notes

- Uses PKCE flow for enhanced security
- Tokens are automatically refreshed
- API validates JWT signatures against Auth0
- Proper CORS configuration for authentication domains

## Development vs Production

The app automatically detects environment and uses appropriate:
- API base addresses
- CORS policies
- Auth0 callback URLs

Make sure to update Auth0 Dashboard URLs when deploying to new environments.
