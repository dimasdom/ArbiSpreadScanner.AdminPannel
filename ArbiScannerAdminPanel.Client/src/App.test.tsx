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
vi.mock('./pages/Account/LoginPage', () => ({ default: () => <div>LoginPageStub</div> }));
vi.mock('./pages/Users/UsersPage', () => ({ default: () => <div>UsersPageStub</div> }));
vi.mock('./pages/Users/UserPage', () => ({ default: () => <div>UserPageStub</div> }));
vi.mock('./pages/Payments/PaymentsPage', () => ({ default: () => <div>PaymentsPageStub</div> }));
vi.mock('./pages/Payments/PaymentPage', () => ({ default: () => <div>PaymentPageStub</div> }));
vi.mock('./pages/Subscriptions/SubscriptionsPage', () => ({ default: () => <div>SubscriptionsPageStub</div> }));
vi.mock('./pages/Subscriptions/SubscriptionPage', () => ({ default: () => <div>SubscriptionPageStub</div> }));
vi.mock('./pages/UserSubscriptions/UserSubscriptionPage', () => ({ default: () => <div>UserSubscriptionPageStub</div> }));
vi.mock('./pages/UserSubscriptions/UserSubscriptionsPage', () => ({ default: () => <div>UserSubscriptionsPageStub</div> }));
vi.mock('./pages/UserSubscriptions/CreateUserSubscriptionPage', () => ({ default: () => <div>CreateUserSubscriptionPageStub</div> }));

const mockUseSelector = vi.fn();
vi.mock('react-redux', () => ({
    useSelector: (selector: (state: unknown) => unknown) => mockUseSelector(selector),
}));

const mockNavigate = vi.fn();
vi.mock('react-router', async () => {
    const actual = await vi.importActual<typeof import('react-router')>('react-router');
    return { ...actual, useNavigate: () => mockNavigate };
});

const mockLogoutMutation = vi.fn();
vi.mock('./store/services/account', () => ({
    useLogoutMutation: () => [mockLogoutMutation],
}));

import App from './App';

describe('App', () => {
    beforeEach(() => {
        mockNavigate.mockReset();
        mockLogoutMutation.mockReset();
    });

    it('redirects to the login page when logged out', () => {
        mockUseSelector.mockImplementation((selector) => selector({ account: { isLoggedIn: false } }));

        render(
            <MemoryRouter initialEntries={['/users']}>
                <App />
            </MemoryRouter>,
        );

        expect(mockNavigate).toHaveBeenCalledWith('/account/login');
    });

    it('renders the users page (index route) when logged in', () => {
        mockUseSelector.mockImplementation((selector) => selector({ account: { isLoggedIn: true } }));

        render(
            <MemoryRouter initialEntries={['/']}>
                <App />
            </MemoryRouter>,
        );

        expect(screen.getByText('UsersPageStub')).toBeInTheDocument();
        expect(mockNavigate).not.toHaveBeenCalled();
    });

    it('renders the login page directly when logged out and already there', () => {
        mockUseSelector.mockImplementation((selector) => selector({ account: { isLoggedIn: false } }));

        render(
            <MemoryRouter initialEntries={['/account/login']}>
                <App />
            </MemoryRouter>,
        );

        expect(screen.getByText('LoginPageStub')).toBeInTheDocument();
    });

    it('wires the NavBar login/logout callbacks', async () => {
        const { default: userEvent } = await import('@testing-library/user-event');
        const user = userEvent.setup();
        mockUseSelector.mockImplementation((selector) => selector({ account: { isLoggedIn: true } }));

        render(
            <MemoryRouter initialEntries={['/']}>
                <App />
            </MemoryRouter>,
        );

        await user.click(screen.getByText('nav-login'));
        expect(mockNavigate).toHaveBeenCalledWith('/account/login');

        await user.click(screen.getByText('nav-logout'));
        expect(mockLogoutMutation).toHaveBeenCalledTimes(1);
    });
});
