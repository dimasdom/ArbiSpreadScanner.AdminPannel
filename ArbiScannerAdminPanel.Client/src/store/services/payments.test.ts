import { describe, expect, it, vi, beforeEach, beforeAll, afterEach } from 'vitest';
import { configureStore } from '@reduxjs/toolkit';

vi.stubEnv('VITE_API_URL', 'http://localhost:5173');

let paymentsAPI: typeof import('./payments').paymentsAPI;

beforeAll(async () => {
    ({ paymentsAPI } = await import('./payments'));
});

const jsonResponse = (status: number, body: unknown) =>
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } });

function makeStore() {
    return configureStore({
        reducer: { [paymentsAPI.reducerPath]: paymentsAPI.reducer },
        middleware: (getDefault) => getDefault({ serializableCheck: false }).concat(paymentsAPI.middleware),
    });
}

describe('paymentsAPI', () => {
    let fetchMock: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        fetchMock = vi.fn();
        vi.stubGlobal('fetch', fetchMock);
    });

    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('getPayments requests the correct paged URL', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: [] }));
        const store = makeStore();

        await store.dispatch(paymentsAPI.endpoints.getPayments.initiate(2));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.url).toBe('http://localhost:5173/api/payments/GetAllPayments?page=2');
        expect(request.method).toBe('GET');
    });

    it('removePayments sends a DELETE with the ids in the body', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: true }));
        const store = makeStore();

        await store.dispatch(paymentsAPI.endpoints.removePayments.initiate([1, 2, 3]));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.method).toBe('DELETE');
        expect(request.url).toBe('http://localhost:5173/api/payments/RemovePayments');
        await expect(request.clone().json()).resolves.toEqual([1, 2, 3]);
    });

    it('getPaymentById requests the correct URL', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: { id: 5 } }));
        const store = makeStore();

        const result = await store.dispatch(paymentsAPI.endpoints.getPaymentById.initiate(5));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.url).toBe('http://localhost:5173/api/payments/GetPaymentById?id=5');
        expect(result.data).toEqual({ isSuccess: true, value: { id: 5 } });
    });
});
