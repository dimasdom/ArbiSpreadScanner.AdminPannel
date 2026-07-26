import { describe, expect, it, vi, beforeEach, beforeAll, afterEach } from 'vitest';
import { configureStore } from '@reduxjs/toolkit';

vi.stubEnv('VITE_API_URL', 'http://localhost:5173');

let subscriptionsAPI: typeof import('./subscriptions').subscriptionsAPI;

beforeAll(async () => {
    ({ subscriptionsAPI } = await import('./subscriptions'));
});

const jsonResponse = (status: number, body: unknown) =>
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } });

function makeStore() {
    return configureStore({
        reducer: { [subscriptionsAPI.reducerPath]: subscriptionsAPI.reducer },
        middleware: (getDefault) => getDefault({ serializableCheck: false }).concat(subscriptionsAPI.middleware),
    });
}

describe('subscriptionsAPI', () => {
    let fetchMock: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        fetchMock = vi.fn();
        vi.stubGlobal('fetch', fetchMock);
    });

    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('getSubscriptions requests the correct paged URL', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: [] }));
        const store = makeStore();

        await store.dispatch(subscriptionsAPI.endpoints.getSubscriptions.initiate(1));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.url).toBe('http://localhost:5173/api/subscriptions/GetAllSubscriptions?page=1');
    });

    it('deleteSubscriptions sends a DELETE with the ids in the body', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: true }));
        const store = makeStore();

        await store.dispatch(subscriptionsAPI.endpoints.deleteSubscriptions.initiate([1]));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.method).toBe('DELETE');
        await expect(request.clone().json()).resolves.toEqual([1]);
    });

    it('getSubscriptionById requests the correct URL', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: { id: 1 } }));
        const store = makeStore();

        await store.dispatch(subscriptionsAPI.endpoints.getSubscriptionById.initiate(1));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.url).toBe('http://localhost:5173/api/subscriptions/GetSubscriptionById?id=1');
    });

    it('updateSubscription posts the subscription body', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: true }));
        const store = makeStore();
        const subscription = { id: 1, type: 'Basic', price: 10, durationInDays: 30 };

        await store.dispatch(subscriptionsAPI.endpoints.updateSubscription.initiate(subscription));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.method).toBe('POST');
        expect(request.url).toBe('http://localhost:5173/api/subscriptions/UpdateSubscription');
        await expect(request.clone().json()).resolves.toEqual(subscription);
    });

    it('createSubscription posts the subscription body', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: {} }));
        const store = makeStore();
        const subscription = { id: 0, type: 'Basic', price: 10, durationInDays: 30 };

        await store.dispatch(subscriptionsAPI.endpoints.createSubscription.initiate(subscription));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.method).toBe('POST');
        expect(request.url).toBe('http://localhost:5173/api/subscriptions/CreateSubscription');
    });
});
