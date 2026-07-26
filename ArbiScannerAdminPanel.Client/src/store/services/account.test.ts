import { describe, expect, it, vi, beforeEach, beforeAll, afterEach } from 'vitest';
import { configureStore } from '@reduxjs/toolkit';
import accountReducer from '../slices/accountSlice';

vi.stubEnv('VITE_API_URL', 'http://localhost:5173');

let accountApi: typeof import('./account').accountApi;

beforeAll(async () => {
    ({ accountApi } = await import('./account'));
});

const jsonResponse = (status: number, body: unknown) =>
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } });

function makeStore() {
    return configureStore({
        reducer: {
            account: accountReducer,
            [accountApi.reducerPath]: accountApi.reducer,
        },
        middleware: (getDefault) => getDefault({ serializableCheck: false }).concat(accountApi.middleware),
    });
}

describe('accountApi', () => {
    let fetchMock: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        fetchMock = vi.fn();
        vi.stubGlobal('fetch', fetchMock);
    });

    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('login: marks the account authenticated on success', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: { token: 't' } }));
        const store = makeStore();

        await store.dispatch(accountApi.endpoints.login.initiate({ userName: 'admin', password: 'password1' }));

        expect(store.getState().account.isLoggedIn).toBe(true);
        expect(store.getState().account.loading).toBe(false);
    });

    it('login: sets an error message when the API reports failure', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: false, errors: [{ message: 'Invalid credentials' }] }));
        const store = makeStore();

        await store.dispatch(accountApi.endpoints.login.initiate({ userName: 'admin', password: 'wrong' }));

        expect(store.getState().account.isLoggedIn).toBe(false);
        expect(store.getState().account.error).toBe('Invalid credentials');
    });

    it('login: sets a network error message when the request throws', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(500, {}));
        const store = makeStore();

        await store.dispatch(accountApi.endpoints.login.initiate({ userName: 'admin', password: 'password1' }));

        expect(store.getState().account.isLoggedIn).toBe(false);
        expect(store.getState().account.error).toBeTruthy();
    });

    it('me: marks authenticated and session-checked on success', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: {} }));
        const store = makeStore();

        await store.dispatch(accountApi.endpoints.me.initiate());

        expect(store.getState().account.isLoggedIn).toBe(true);
        expect(store.getState().account.sessionChecked).toBe(true);
    });

    it('me: logs out and marks session-checked when the API reports failure', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: false }));
        const store = makeStore();

        await store.dispatch(accountApi.endpoints.me.initiate());

        expect(store.getState().account.isLoggedIn).toBe(false);
        expect(store.getState().account.sessionChecked).toBe(true);
    });

    it('me: logs out and marks session-checked when the request throws', async () => {
        fetchMock.mockRejectedValueOnce(new Error('network down'));
        const store = makeStore();

        await store.dispatch(accountApi.endpoints.me.initiate());

        expect(store.getState().account.isLoggedIn).toBe(false);
        expect(store.getState().account.sessionChecked).toBe(true);
    });

    it('logout: clears the logged-in state', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, {}));
        const store = makeStore();
        store.dispatch({ type: 'account/setAuthenticated' });

        await store.dispatch(accountApi.endpoints.logout.initiate());

        expect(store.getState().account.isLoggedIn).toBe(false);
    });

});
