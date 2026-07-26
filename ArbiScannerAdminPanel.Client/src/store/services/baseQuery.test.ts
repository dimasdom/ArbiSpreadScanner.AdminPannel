import { describe, expect, it, vi, beforeEach, beforeAll, afterEach } from 'vitest';
import type { BaseQueryApi } from '@reduxjs/toolkit/query';
import { logout } from '../slices/accountSlice';

vi.stubEnv('VITE_API_URL', 'http://localhost:5173');
let baseQueryWithReauth: typeof import('./baseQuery').baseQueryWithReauth;

beforeAll(async () => {
    ({ baseQueryWithReauth } = await import('./baseQuery'));
});

const jsonResponse = (status: number, body: unknown) =>
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } });

const createApi = (): BaseQueryApi => ({
    signal: new AbortController().signal,
    abort: vi.fn(),
    dispatch: vi.fn(),
    getState: vi.fn(() => ({})),
    extra: undefined,
    endpoint: 'test',
    type: 'query',
    forced: false,
});

describe('baseQueryWithReauth', () => {
    let fetchMock: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        fetchMock = vi.fn();
        vi.stubGlobal('fetch', fetchMock);
    });

    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('passes through a successful response without side effects', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true }));
        const api = createApi();

        const result = await baseQueryWithReauth('/Users/GetClientUsers?page=1', api, {});

        expect(result.data).toEqual({ isSuccess: true });
        expect(fetchMock).toHaveBeenCalledTimes(1);
        expect(api.dispatch).not.toHaveBeenCalled();
    });

    it('retries the original request after a successful refresh on a 401', async () => {
        fetchMock
            .mockResolvedValueOnce(jsonResponse(401, {}))
            .mockResolvedValueOnce(jsonResponse(200, {}))
            .mockResolvedValueOnce(jsonResponse(200, { isSuccess: true }));
        const api = createApi();

        const result = await baseQueryWithReauth('/Users/GetClientUsers?page=1', api, {});

        expect(fetchMock).toHaveBeenCalledTimes(3);
        const refreshCallUrl = String((fetchMock.mock.calls[1][0] as Request).url ?? fetchMock.mock.calls[1][0]);
        expect(refreshCallUrl).toContain('/Account/Refresh');
        expect(result.data).toEqual({ isSuccess: true });
        expect(api.dispatch).not.toHaveBeenCalled();
    });

    it('dispatches logout when the refresh call itself fails', async () => {
        fetchMock
            .mockResolvedValueOnce(jsonResponse(401, {}))
            .mockResolvedValueOnce(jsonResponse(401, {}));
        const api = createApi();

        const result = await baseQueryWithReauth('/Users/GetClientUsers?page=1', api, {});

        expect(fetchMock).toHaveBeenCalledTimes(2);
        expect(api.dispatch).toHaveBeenCalledWith(logout());
        expect(result.error).toBeDefined();
    });

    it('does not attempt a refresh for the Authenticate endpoint', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(401, {}));
        const api = createApi();

        const result = await baseQueryWithReauth('/Account/Authenticate', api, {});

        expect(fetchMock).toHaveBeenCalledTimes(1);
        expect(api.dispatch).toHaveBeenCalledWith(logout());
        expect(result.error).toBeDefined();
    });

    it('dispatches logout on a 403 without attempting refresh loops indefinitely', async () => {
        fetchMock
            .mockResolvedValueOnce(jsonResponse(403, {}))
            .mockResolvedValueOnce(jsonResponse(200, {}))
            .mockResolvedValueOnce(jsonResponse(403, {}));
        const api = createApi();

        const result = await baseQueryWithReauth({ url: '/payments/GetAllPayments' }, api, {});

        expect(api.dispatch).toHaveBeenCalledWith(logout());
        expect(result.error).toBeDefined();
    });
});
