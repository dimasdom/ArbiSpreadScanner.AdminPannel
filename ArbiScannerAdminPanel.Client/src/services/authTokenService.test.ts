import { beforeEach, describe, expect, it } from 'vitest';
import { getAccessToken, setTokenGetter } from './authTokenService';

describe('authTokenService', () => {
    beforeEach(() => {
        setTokenGetter(() => null);
    });

    it('returns null before a getter has been registered', () => {
        expect(getAccessToken()).toBeNull();
    });

    it('returns the token produced by the registered getter', () => {
        setTokenGetter(() => 'access-token-123');

        expect(getAccessToken()).toBe('access-token-123');
    });

    it('reflects updates when the getter is replaced', () => {
        setTokenGetter(() => 'first-token');
        expect(getAccessToken()).toBe('first-token');

        setTokenGetter(() => 'second-token');
        expect(getAccessToken()).toBe('second-token');
    });

    it('invokes the getter on every call rather than caching', () => {
        let counter = 0;
        setTokenGetter(() => `token-${++counter}`);

        expect(getAccessToken()).toBe('token-1');
        expect(getAccessToken()).toBe('token-2');
    });
});
