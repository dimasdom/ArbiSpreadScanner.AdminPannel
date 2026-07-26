import { describe, expect, it, vi, beforeAll } from 'vitest';

vi.stubEnv('VITE_API_URL', 'http://localhost:5173');

let store: typeof import('./store').default;
let accountApi: typeof import('./services/account').accountApi;
let subscriptionsAPI: typeof import('./services/subscriptions').subscriptionsAPI;
let usersAPI: typeof import('./services/users').usersAPI;
let paymentsAPI: typeof import('./services/payments').paymentsAPI;
let userSubscriptionsAPI: typeof import('./services/userSubscriptions').userSubscriptionsAPI;

beforeAll(async () => {
    ({ default: store } = await import('./store'));
    ({ accountApi } = await import('./services/account'));
    ({ subscriptionsAPI } = await import('./services/subscriptions'));
    ({ usersAPI } = await import('./services/users'));
    ({ paymentsAPI } = await import('./services/payments'));
    ({ userSubscriptionsAPI } = await import('./services/userSubscriptions'));
});

describe('store', () => {
    it('wires up the account slice and every API reducer', () => {
        const state = store.getState() as unknown as Record<string, unknown>;

        expect(state.account).toEqual({ isLoggedIn: false, loading: false, error: null, sessionChecked: false });
        expect(state[accountApi.reducerPath]).toBeDefined();
        expect(state[subscriptionsAPI.reducerPath]).toBeDefined();
        expect(state[usersAPI.reducerPath]).toBeDefined();
        expect(state[paymentsAPI.reducerPath]).toBeDefined();
        expect(state[userSubscriptionsAPI.reducerPath]).toBeDefined();
    });

    it('dispatches account actions through the persisted reducer', async () => {
        const { setAuthenticated } = await import('./slices/accountSlice');
        store.dispatch(setAuthenticated());

        expect(store.getState().account.isLoggedIn).toBe(true);
    });
});
