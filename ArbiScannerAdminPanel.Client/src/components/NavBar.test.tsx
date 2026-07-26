import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import NavBar from './NavBar';

describe('NavBar', () => {
    it('shows nav links and a logout button when logged in', () => {
        render(
            <MemoryRouter>
                <NavBar isLoggedIn={true} onLogin={vi.fn()} onLogout={vi.fn()} />
            </MemoryRouter>,
        );

        expect(screen.getAllByText('Users')[0]).toBeInTheDocument();
        expect(screen.getAllByText('Log out')[0]).toBeInTheDocument();
    });

    it('hides the desktop nav links and shows a login button when logged out', () => {
        render(
            <MemoryRouter>
                <NavBar isLoggedIn={false} onLogin={vi.fn()} onLogout={vi.fn()} />
            </MemoryRouter>,
        );

        // The mobile side menu always renders nav links; only the desktop bar hides them when logged out.
        expect(screen.getAllByText('Users')).toHaveLength(1);
        expect(screen.getAllByText('Log in')[0]).toBeInTheDocument();
    });

    it('invokes onLogout when the desktop logout button is clicked', async () => {
        const user = userEvent.setup();
        const onLogout = vi.fn();
        render(
            <MemoryRouter>
                <NavBar isLoggedIn={true} onLogin={vi.fn()} onLogout={onLogout} />
            </MemoryRouter>,
        );

        await user.click(screen.getAllByText('Log out')[0]);

        expect(onLogout).toHaveBeenCalledTimes(1);
    });

    it('invokes onLogin when the desktop login button is clicked', async () => {
        const user = userEvent.setup();
        const onLogin = vi.fn();
        render(
            <MemoryRouter>
                <NavBar isLoggedIn={false} onLogin={onLogin} onLogout={vi.fn()} />
            </MemoryRouter>,
        );

        await user.click(screen.getAllByText('Log in')[0]);

        expect(onLogin).toHaveBeenCalledTimes(1);
    });

    it('toggles the mobile side menu and closes it via the overlay', async () => {
        const user = userEvent.setup();
        render(
            <MemoryRouter>
                <NavBar isLoggedIn={true} onLogin={vi.fn()} onLogout={vi.fn()} />
            </MemoryRouter>,
        );

        await user.click(screen.getByLabelText('Toggle menu'));
        expect(screen.getByLabelText('Close menu')).toBeInTheDocument();

        await user.click(screen.getByLabelText('Close menu'));
    });

    it('invokes onLogout from the side menu and closes it', async () => {
        const user = userEvent.setup();
        const onLogout = vi.fn();
        render(
            <MemoryRouter>
                <NavBar isLoggedIn={true} onLogin={vi.fn()} onLogout={onLogout} />
            </MemoryRouter>,
        );

        await user.click(screen.getByLabelText('Toggle menu'));
        await user.click(screen.getAllByText('Log out')[1]);

        expect(onLogout).toHaveBeenCalledTimes(1);
    });

    it('invokes onLogin from the side menu when logged out', async () => {
        const user = userEvent.setup();
        const onLogin = vi.fn();
        render(
            <MemoryRouter>
                <NavBar isLoggedIn={false} onLogin={onLogin} onLogout={vi.fn()} />
            </MemoryRouter>,
        );

        await user.click(screen.getByLabelText('Toggle menu'));
        await user.click(screen.getAllByText('Log in')[1]);

        expect(onLogin).toHaveBeenCalledTimes(1);
    });
});
