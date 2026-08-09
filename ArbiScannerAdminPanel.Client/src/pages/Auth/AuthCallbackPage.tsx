import { useEffect } from 'react';
import { useNavigate } from 'react-router';
import { useAuth } from 'react-oidc-context';

// Keycloak's redirect_uri must be one fixed, whitelisted string. AuthProvider
// (see main.tsx) processes the ?code=&state= callback automatically; this
// page just waits for that to finish and then hands off to the normal app.
export function AuthCallbackPage() {
    const auth = useAuth();
    const navigate = useNavigate();

    useEffect(() => {
        if (!auth.isLoading && auth.isAuthenticated) {
            navigate('/', { replace: true });
        }
    }, [auth.isLoading, auth.isAuthenticated, navigate]);

    if (auth.error) {
        return (
            <div className="flex flex-col items-center justify-center min-h-64 mt-6 gap-4">
                <p className="text-red-600">Sign-in failed.</p>
                <p className="text-sm text-gray-500">{auth.error.message}</p>
            </div>
        );
    }

    return (
        <div className="flex items-center justify-center min-h-64 mt-6">
            <div className="w-10 h-10 border-4 border-gray-200 border-t-indigo-500 rounded-full animate-spin" />
        </div>
    );
}

export default AuthCallbackPage;
