window.AuthenticationService = {
    init: function (options) {
        // Initialize authentication service with OIDC options
        this.authority = options.authority;
        this.client_id = options.client_id;
        this.redirect_uri = options.redirect_uri;
        this.post_logout_redirect_uri = options.post_logout_redirect_uri;
        this.response_type = options.response_type;
        this.scope = options.scope;
        console.log('AuthenticationService initialized with options:', options);
        return Promise.resolve();
    },
    
    getUser: function () {
        // Return user from localStorage or null
        const user = localStorage.getItem('oidc.user');
        return Promise.resolve(user ? JSON.parse(user) : null);
    },
    
    removeUser: function () {
        // Remove user from localStorage
        localStorage.removeItem('oidc.user');
        return Promise.resolve();
    },
    
    signinRedirect: function (args) {
        // Redirect to Auth0 for sign in
        const state = Math.random().toString(36).substring(2, 15);
        const nonce = Math.random().toString(36).substring(2, 15);
        
        localStorage.setItem('auth_state', state);
        localStorage.setItem('auth_nonce', nonce);
        
        const params = new URLSearchParams({
            client_id: this.client_id,
            redirect_uri: this.redirect_uri,
            response_type: this.response_type,
            scope: this.scope,
            state: state,
            nonce: nonce
        });
        
        if (args && args.audience) {
            params.append('audience', args.audience);
        }
        
        const url = `${this.authority}/authorize?${params.toString()}`;
        window.location.href = url;
        return Promise.resolve();
    },
    
    signoutRedirect: function (args) {
        // Redirect to Auth0 for sign out
        this.removeUser();
        const params = new URLSearchParams({
            client_id: this.client_id,
            returnTo: this.post_logout_redirect_uri
        });
        
        const url = `${this.authority}/v2/logout?${params.toString()}`;
        window.location.href = url;
        return Promise.resolve();
    },
    
    signinRedirectCallback: function () {
        // Handle callback from Auth0
        const urlParams = new URLSearchParams(window.location.search);
        const code = urlParams.get('code');
        const state = urlParams.get('state');
        const error = urlParams.get('error');
        
        if (error) {
            console.error('Authentication error:', error);
            return Promise.reject(new Error(error));
        }
        
        if (state !== localStorage.getItem('auth_state')) {
            return Promise.reject(new Error('Invalid state parameter'));
        }
        
        if (code) {
            // For now, just store a minimal user object
            // In a real implementation, you'd exchange the code for tokens
            const user = {
                access_token: 'dummy_token',
                id_token: 'dummy_id_token',
                profile: {
                    sub: 'auth0|123456',
                    name: 'Test User',
                    email: 'test@example.com'
                },
                expires_at: Date.now() + 3600000 // 1 hour
            };
            
            localStorage.setItem('oidc.user', JSON.stringify(user));
            localStorage.removeItem('auth_state');
            localStorage.removeItem('auth_nonce');
            
            return Promise.resolve(user);
        }
        
        return Promise.reject(new Error('No authorization code received'));
    }
};
