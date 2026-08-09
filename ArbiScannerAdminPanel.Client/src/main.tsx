import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { BrowserRouter } from 'react-router'
import store from './store/store.ts'
import { Provider } from 'react-redux'
import { AuthProvider } from 'react-oidc-context'
import { oidcUserManager } from './services/oidcUserManager.ts'

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <AuthProvider
            userManager={oidcUserManager}
            onSigninCallback={() => {
                // Strip ?code=&state= so a re-render doesn't re-process the same
                // callback (oidc-client-ts would reject it: state already consumed).
                window.history.replaceState({}, document.title, window.location.pathname);
            }}
        >
            <Provider store={store}>
                <BrowserRouter basename={import.meta.env.BASE_URL}>
                    <App />
                </BrowserRouter>
            </Provider>
        </AuthProvider>
    </StrictMode>,
)
