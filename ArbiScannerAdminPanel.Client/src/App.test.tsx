import { describe, expect, it, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';

vi.mock('./components/NavBar', () => ({
    default: ({ isLoggedIn, onLogin, onLogout }: { isLoggedIn: boolean; onLogin: () => void; onLogout: () => void }) => (
        <div>
            <span>NavBar isLoggedIn={String(isLoggedIn)}</span>
            <button onClick={onLogin}>nav-login</button>
            <button onClick={onLogout}>nav-logout</button>
        </div>
    ),
}));
vi.mock('./pages/Auth/AuthCallbackPage', () => ({ default: () => <div>AuthCallbackPageStub</div> }));
vi.mock('./pages/Users/UsersPage', () => ({ default: () => <div>UsersPageStub</div> }));
vi.mock('./pages/Users/UserPage', () => ({ default: () => <div>UserPageStub</div> }));
vi.mock('./pages/Payments/PaymentsPage', () => ({ default: () => <div>PaymentsPageStub</div> }));
vi.mock('./pages/Payments/PaymentPage', () => ({ default: () => <div>PaymentPageStub</div> }));
vi.mock('./pages/Subscriptions/SubscriptionsPage', () => ({ default: () => <div>SubscriptionsPageStub</div> }));
vi.mock('./pages/Subscriptions/SubscriptionPage', () => ({ default: () => <div>SubscriptionPageStub</div> }));
vi.mock('./pages/UserSubscriptions/UserSubscriptionPage', () => ({ default: () => <div>UserSubscriptionPageStub</div> }));
vi.mock('./pages/UserSubscriptions/UserSubscriptionsPage', () => ({ default: () => <div>UserSubscriptionsPageStub</div> }));
vi.mock('./pages/UserSubscriptions/CreateUserSubscriptionPage', () => ({ default: () => <div>CreateUserSubscriptionPageStub</div> }));

const useAuthMock = vi.fn();
const signinRedirectMock = vi.fn();
const signoutRedirectMock = vi.fn();
vi.mock('react-oidc-context', () => ({
    useAuth: () => useAuthMock(),
}));

import App from './App';

function mockAuth(overrides: Partial<{ isLoading: boolean; isAuthenticated: boolean }> = {}) {
    useAuthMock.mockReturnValue({
        isLoading: false,
        isAuthenticated: false,
        signinRedirect: signinRedirectMock,
        signoutRedirect: signoutRedirectMock,
        ...overrides,
    });
}

describe('App', () => {
    beforeEach(() => {
        signinRedirectMock.mockReset();
        signoutRedirectMock.mockReset();
    });

    it('redirects to Keycloak when logged out', () => {
        mockAuth({ isAuthenticated: false });

        render(
            <MemoryRouter initialEntries={['/users']}>
                <App />
            </MemoryRouter>,
        );

        expect(signinRedirectMock).toHaveBeenCalledTimes(1);
    });

    it('renders the users page (index route) when logged in', () => {
        mockAuth({ isAuthenticated: true });

        render(
            <MemoryRouter initialEntries={['/']}>
                <App />
            </MemoryRouter>,
        );

        expect(screen.getByText('UsersPageStub')).toBeInTheDocument();
        expect(signinRedirectMock).not.toHaveBeenCalled();
    });

    it('does not redirect while still loading the auth session', () => {
        mockAuth({ isLoading: true, isAuthenticated: false });

        render(
            <MemoryRouter initialEntries={['/users']}>
                <App />
            </MemoryRouter>,
        );

        expect(signinRedirectMock).not.toHaveBeenCalled();
    });

    it('renders the auth callback page without redirecting, even when logged out', () => {
        mockAuth({ isAuthenticated: false });

        render(
            <MemoryRouter initialEntries={['/auth/callback']}>
                <App />
            </MemoryRouter>,
        );

        expect(screen.getByText('AuthCallbackPageStub')).toBeInTheDocument();
        expect(signinRedirectMock).not.toHaveBeenCalled();
    });

    it('wires the NavBar login/logout callbacks', async () => {
        const { default: userEvent } = await import('@testing-library/user-event');
        const user = userEvent.setup();
        mockAuth({ isAuthenticated: true });

        render(
            <MemoryRouter initialEntries={['/']}>
                <App />
            </MemoryRouter>,
        );

        await user.click(screen.getByText('nav-login'));
        expect(signinRedirectMock).toHaveBeenCalledTimes(1);

        await user.click(screen.getByText('nav-logout'));
        expect(signoutRedirectMock).toHaveBeenCalledTimes(1);
    });
});
