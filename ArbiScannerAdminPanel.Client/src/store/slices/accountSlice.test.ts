import { describe, expect, it } from 'vitest';
import reducer, {
    clearError,
    logout,
    markSessionChecked,
    setAuthenticated,
    setError,
    setLoading,
    type AccountState,
} from './accountSlice';

const initialState: AccountState = {
    isLoggedIn: false,
    loading: false,
    error: null,
    sessionChecked: false,
};

describe('accountSlice', () => {
    it('returns the initial state', () => {
        expect(reducer(undefined, { type: 'unknown' })).toEqual(initialState);
    });

    it('setAuthenticated marks logged in, clears loading/error, and marks session checked', () => {
        const state: AccountState = { isLoggedIn: false, loading: true, error: 'boom', sessionChecked: false };

        const result = reducer(state, setAuthenticated());

        expect(result).toEqual({ isLoggedIn: true, loading: false, error: null, sessionChecked: true });
    });

    it('setLoading updates the loading flag', () => {
        const result = reducer(initialState, setLoading(true));

        expect(result.loading).toBe(true);
    });

    it('setError sets the error message', () => {
        const result = reducer(initialState, setError('failed to log in'));

        expect(result.error).toBe('failed to log in');
    });

    it('clearError resets the error to null', () => {
        const state: AccountState = { ...initialState, error: 'some error' };

        const result = reducer(state, clearError());

        expect(result.error).toBeNull();
    });

    it('markSessionChecked sets sessionChecked to true without touching other fields', () => {
        const result = reducer(initialState, markSessionChecked());

        expect(result).toEqual({ ...initialState, sessionChecked: true });
    });

    it('logout resets isLoggedIn/loading/error and marks session checked', () => {
        const state: AccountState = { isLoggedIn: true, loading: true, error: 'stale error', sessionChecked: false };

        const result = reducer(state, logout());

        expect(result).toEqual({ isLoggedIn: false, loading: false, error: null, sessionChecked: true });
    });
});
