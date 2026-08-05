import { describe, expect, it, beforeAll } from 'vitest';

let store: typeof import('./store').default;
let subscriptionsAPI: typeof import('./services/subscriptions').subscriptionsAPI;
let usersAPI: typeof import('./services/users').usersAPI;
let paymentsAPI: typeof import('./services/payments').paymentsAPI;
let userSubscriptionsAPI: typeof import('./services/userSubscriptions').userSubscriptionsAPI;

beforeAll(async () => {
    ({ default: store } = await import('./store'));
    ({ subscriptionsAPI } = await import('./services/subscriptions'));
    ({ usersAPI } = await import('./services/users'));
    ({ paymentsAPI } = await import('./services/payments'));
    ({ userSubscriptionsAPI } = await import('./services/userSubscriptions'));
});

describe('store', () => {
    it('wires up every API reducer', () => {
        const state = store.getState() as unknown as Record<string, unknown>;

        expect(state[subscriptionsAPI.reducerPath]).toBeDefined();
        expect(state[usersAPI.reducerPath]).toBeDefined();
        expect(state[paymentsAPI.reducerPath]).toBeDefined();
        expect(state[userSubscriptionsAPI.reducerPath]).toBeDefined();
    });
});
