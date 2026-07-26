import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

const mockUseLoginForm = vi.fn();
vi.mock('./hooks/useLoginForm', () => ({
    useLoginForm: () => mockUseLoginForm(),
}));

import LoginPage from './LoginPage';

describe('LoginPage', () => {
    it('renders field errors and the login error message', () => {
        mockUseLoginForm.mockReturnValue({
            usernameRef: { current: null },
            passwordRef: { current: null },
            errors: { username: 'Username must be at least 3 characters long.', password: '' },
            loading: false,
            loginError: 'Invalid credentials',
            clearFieldError: vi.fn(),
            handleSubmit: vi.fn((e: React.FormEvent) => e.preventDefault()),
        });

        render(<LoginPage />);

        expect(screen.getByText('Username must be at least 3 characters long.')).toBeInTheDocument();
        expect(screen.getByText('Invalid credentials')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Login' })).toBeInTheDocument();
    });

    it('shows a loading state and calls clearFieldError while typing', async () => {
        const user = userEvent.setup();
        const clearFieldError = vi.fn();
        mockUseLoginForm.mockReturnValue({
            usernameRef: { current: null },
            passwordRef: { current: null },
            errors: { username: '', password: '' },
            loading: true,
            loginError: null,
            clearFieldError,
            handleSubmit: vi.fn((e: React.FormEvent) => e.preventDefault()),
        });

        render(<LoginPage />);

        expect(screen.getByRole('button', { name: 'Signing in...' })).toBeDisabled();
        await user.type(screen.getByLabelText('Username'), 'a');

        expect(clearFieldError).toHaveBeenCalledWith('username');
    });

    it('calls handleSubmit when the form is submitted', async () => {
        const user = userEvent.setup();
        const handleSubmit = vi.fn((e: React.FormEvent) => e.preventDefault());
        mockUseLoginForm.mockReturnValue({
            usernameRef: { current: null },
            passwordRef: { current: null },
            errors: { username: '', password: '' },
            loading: false,
            loginError: null,
            clearFieldError: vi.fn(),
            handleSubmit,
        });

        render(<LoginPage />);
        await user.type(screen.getByLabelText('Username'), 'admin');
        await user.type(screen.getByLabelText('Password'), 'password1');
        await user.click(screen.getByRole('button', { name: 'Login' }));

        expect(handleSubmit).toHaveBeenCalledTimes(1);
    });
});
