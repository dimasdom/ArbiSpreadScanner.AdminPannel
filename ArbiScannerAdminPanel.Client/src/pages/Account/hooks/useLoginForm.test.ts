import { describe, expect, it, vi, beforeEach } from 'vitest';
import { act, renderHook } from '@testing-library/react';

const mockNavigate = vi.fn();
vi.mock('react-router', () => ({
    useNavigate: () => mockNavigate,
}));

const mockDispatch = vi.fn();
const mockUseSelector = vi.fn();
vi.mock('react-redux', () => ({
    useDispatch: () => mockDispatch,
    useSelector: (selector: (state: unknown) => unknown) => mockUseSelector(selector),
}));

const mockLogin = vi.fn();
const mockUseLoginMutation = vi.fn();
vi.mock('../../../store/services/account', () => ({
    useLoginMutation: () => mockUseLoginMutation(),
}));

import { useLoginForm } from './useLoginForm';

const state = { account: { isLoggedIn: false, loading: false, error: null, sessionChecked: true } };

describe('useLoginForm', () => {
    beforeEach(() => {
        mockNavigate.mockReset();
        mockDispatch.mockReset();
        mockLogin.mockReset();
        mockUseLoginMutation.mockReturnValue([mockLogin, { isLoading: false }]);
        mockUseSelector.mockImplementation((selector: (s: typeof state) => unknown) => selector(state));
    });

    it('clears the account error and does not navigate when logged out', () => {
        renderHook(() => useLoginForm());

        expect(mockDispatch).toHaveBeenCalledTimes(1);
        expect(mockNavigate).not.toHaveBeenCalled();
    });

    it('navigates home when already logged in', () => {
        mockUseSelector.mockImplementation((selector: (s: typeof state) => unknown) =>
            selector({ account: { ...state.account, isLoggedIn: true } }),
        );

        renderHook(() => useLoginForm());

        expect(mockNavigate).toHaveBeenCalledWith('/');
    });

    it('rejects submission with a short username and password', () => {
        const { result } = renderHook(() => useLoginForm());
        Object.defineProperty(result.current.usernameRef, 'current', { value: { value: 'ab' }, writable: true });
        Object.defineProperty(result.current.passwordRef, 'current', { value: { value: 'short' }, writable: true });

        act(() => {
            result.current.handleSubmit({ preventDefault: vi.fn() } as unknown as React.FormEvent<HTMLFormElement>);
        });

        expect(result.current.errors.username).toContain('at least 3');
        expect(result.current.errors.password).toContain('at least 8');
        expect(mockLogin).not.toHaveBeenCalled();
    });

    it('submits valid credentials', () => {
        const { result } = renderHook(() => useLoginForm());
        Object.defineProperty(result.current.usernameRef, 'current', { value: { value: 'admin' }, writable: true });
        Object.defineProperty(result.current.passwordRef, 'current', { value: { value: 'password1' }, writable: true });

        act(() => {
            result.current.handleSubmit({ preventDefault: vi.fn() } as unknown as React.FormEvent<HTMLFormElement>);
        });

        expect(mockLogin).toHaveBeenCalledWith({ userName: 'admin', password: 'password1' });
    });

    it('clearFieldError resets a single field error', () => {
        const { result } = renderHook(() => useLoginForm());
        Object.defineProperty(result.current.usernameRef, 'current', { value: { value: '' }, writable: true });
        Object.defineProperty(result.current.passwordRef, 'current', { value: { value: '' }, writable: true });

        act(() => {
            result.current.handleSubmit({ preventDefault: vi.fn() } as unknown as React.FormEvent<HTMLFormElement>);
        });
        expect(result.current.errors.username).not.toBe('');

        act(() => {
            result.current.clearFieldError('username');
        });

        expect(result.current.errors.username).toBe('');
    });
});
