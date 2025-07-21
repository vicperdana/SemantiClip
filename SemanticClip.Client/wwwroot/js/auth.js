window.AuthenticationService = {
    auth0Client: null,
    isInitialized: false,
    
    init: function (options) {
        console.log('AuthenticationService.init called with options:', options);
        
        if (this.isInitialized) {
            console.log('AuthenticationService already initialized');
            return Promise.resolve();
        }
        
        // Check if Auth0 is available
        if (typeof window.auth0 === 'undefined') {
            console.warn('Auth0 SPA SDK not available, authentication will be limited');
            this.isInitialized = true;
            return Promise.resolve();
        }
        
        try {
            const auth0Options = {
                domain: options.authority.replace('https://', '').replace('/', ''),
                clientId: options.client_id,
                authorizationParams: {
                    redirect_uri: options.redirect_uri,
                    audience: options.additionalProviderParameters?.audience,
                    scope: options.scope || 'openid profile email'
                }
            };
            
            console.log('Creating Auth0 client with options:', auth0Options);
            
            return window.auth0.createAuth0Client(auth0Options)
                .then(client => {
                    this.auth0Client = client;
                    this.isInitialized = true;
                    console.log('Auth0 client initialized successfully');
                    return Promise.resolve();
                })
                .catch(error => {
                    console.error('Failed to initialize Auth0 client:', error);
                    // Don't fail the app, just mark as initialized without Auth0
                    this.isInitialized = true;
                    return Promise.resolve();
                });
        } catch (error) {
            console.error('Error during Auth0 initialization:', error);
            this.isInitialized = true;
            return Promise.resolve();
        }
    },
    
    getUser: function () {
        console.log('AuthenticationService.getUser called');
        
        if (!this.auth0Client) {
            console.log('Auth0 client not initialized, returning null');
            return Promise.resolve(null);
        }
        
        return this.auth0Client.getUser()
            .then(user => {
                console.log('Auth0 user retrieved:', user);
                return user;
            })
            .catch(error => {
                console.error('Failed to get user from Auth0:', error);
                return null;
            });
    },
    
    removeUser: function () {
        console.log('AuthenticationService.removeUser called');
        // Auth0 handles user state internally, no action needed
        return Promise.resolve();
    },
    
    signinRedirect: function (args) {
        if (!this.auth0Client) {
            return Promise.reject(new Error('Auth0 client not initialized'));
        }
        
        const loginOptions = {};
        if (args && args.audience) {
            loginOptions.authorizationParams = {
                audience: args.audience
            };
        }
        
        console.log('Starting Auth0 login with options:', loginOptions);
        
        return this.auth0Client.loginWithRedirect(loginOptions)
            .catch(error => {
                console.error('Auth0 login failed:', error);
                return Promise.reject(error);
            });
    },
    
    signoutRedirect: function (args) {
        if (!this.auth0Client) {
            return Promise.reject(new Error('Auth0 client not initialized'));
        }
        
        const logoutOptions = {
            logoutParams: {
                returnTo: args?.returnTo || window.location.origin
            }
        };
        
        console.log('Starting Auth0 logout with options:', logoutOptions);
        
        return this.auth0Client.logout(logoutOptions)
            .catch(error => {
                console.error('Auth0 logout failed:', error);
                return Promise.reject(error);
            });
    },
    
    signinRedirectCallback: function () {
        if (!this.auth0Client) {
            return Promise.reject(new Error('Auth0 client not initialized'));
        }
        
        console.log('Handling Auth0 login callback');
        
        return this.auth0Client.handleRedirectCallback()
            .then(result => {
                console.log('Auth0 callback handled successfully:', result);
                
                // Get the authenticated user
                return this.auth0Client.getUser().then(user => {
                    console.log('User after callback:', user);
                    return user;
                });
            })
            .catch(error => {
                console.error('Auth0 callback handling failed:', error);
                return Promise.reject(error);
            });
    },
    
    getAccessToken: function () {
        if (!this.auth0Client) {
            return Promise.resolve(null);
        }
        
        return this.auth0Client.getTokenSilently()
            .then(token => {
                console.log('Got access token from Auth0');
                return token;
            })
            .catch(error => {
                console.error('Failed to get access token:', error);
                return null;
            });
    },
    
    isAuthenticated: function () {
        if (!this.auth0Client) {
            return Promise.resolve(false);
        }
        
        return this.auth0Client.isAuthenticated()
            .then(isAuth => {
                console.log('Is authenticated:', isAuth);
                return isAuth;
            })
            .catch(error => {
                console.error('Failed to check authentication status:', error);
                return false;
            });
    }
};
