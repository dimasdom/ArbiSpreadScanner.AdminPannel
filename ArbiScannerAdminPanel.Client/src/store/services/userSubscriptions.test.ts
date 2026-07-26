import { describe, expect, it, vi, beforeEach, beforeAll, afterEach } from 'vitest';
import { configureStore } from '@reduxjs/toolkit';

vi.stubEnv('VITE_API_URL', 'http://localhost:5173');

let userSubscriptionsAPI: typeof import('./userSubscriptions').userSubscriptionsAPI;

beforeAll(async () => {
    ({ userSubscriptionsAPI } = await import('./userSubscriptions'));
});

const jsonResponse = (status: number, body: unknown) =>
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } });

function makeStore() {
    return configureStore({
        reducer: { [userSubscriptionsAPI.reducerPath]: userSubscriptionsAPI.reducer },
        middleware: (getDefault) => getDefault({ serializableCheck: false }).concat(userSubscriptionsAPI.middleware),
    });
}

describe('userSubscriptionsAPI', () => {
    let fetchMock: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        fetchMock = vi.fn();
        vi.stubGlobal('fetch', fetchMock);
    });

    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('getUserSubscriptions requests the correct paged URL', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: [] }));
        const store = makeStore();

        await store.dispatch(userSubscriptionsAPI.endpoints.getUserSubscriptions.initiate(1));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.url).toBe('http://localhost:5173/api/subscriptions/GetAllUserSubscriptions?page=1');
    });

    it('deleteUserSubscriptions sends a DELETE with the ids in the body', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: true }));
        const store = makeStore();

        await store.dispatch(userSubscriptionsAPI.endpoints.deleteUserSubscriptions.initiate([1]));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.method).toBe('DELETE');
        await expect(request.clone().json()).resolves.toEqual([1]);
    });

    it('getUserSubscriptionById requests the correct URL', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: { id: 1 } }));
        const store = makeStore();

        await store.dispatch(userSubscriptionsAPI.endpoints.getUserSubscriptionById.initiate(1));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.url).toBe('http://localhost:5173/api/subscriptions/GetUserSubscriptionById?Id=1');
    });

    it('updateUserSubscription posts the subscription body', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: true }));
        const store = makeStore();
        const model = { id: 1, userId: 'u1', subscriptionId: 1, startDate: '2024-01-01', endDate: '2024-02-01' };

        await store.dispatch(userSubscriptionsAPI.endpoints.updateUserSubscription.initiate(model));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.method).toBe('POST');
        expect(request.url).toBe('http://localhost:5173/api/subscriptions/UpdateUserSubscription');
    });

    it('createUserSubscription posts the create DTO', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: {} }));
        const store = makeStore();
        const dto = { userEmail: 'a@test.com', subscriptionId: 1 };

        await store.dispatch(userSubscriptionsAPI.endpoints.createUserSubscription.initiate(dto));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.method).toBe('POST');
        expect(request.url).toBe('http://localhost:5173/api/subscriptions/CreateUserSubscription');
        await expect(request.clone().json()).resolves.toEqual(dto);
    });
});
