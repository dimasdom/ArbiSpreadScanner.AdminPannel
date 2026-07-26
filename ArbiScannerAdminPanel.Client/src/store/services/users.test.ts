import { describe, expect, it, vi, beforeEach, beforeAll, afterEach } from 'vitest';
import { configureStore } from '@reduxjs/toolkit';

vi.stubEnv('VITE_API_URL', 'http://localhost:5173');

let usersAPI: typeof import('./users').usersAPI;

beforeAll(async () => {
    ({ usersAPI } = await import('./users'));
});

const jsonResponse = (status: number, body: unknown) =>
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } });

function makeStore() {
    return configureStore({
        reducer: { [usersAPI.reducerPath]: usersAPI.reducer },
        middleware: (getDefault) => getDefault({ serializableCheck: false }).concat(usersAPI.middleware),
    });
}

describe('usersAPI', () => {
    let fetchMock: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        fetchMock = vi.fn();
        vi.stubGlobal('fetch', fetchMock);
    });

    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('getUsers requests the correct paged URL', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: [] }));
        const store = makeStore();

        await store.dispatch(usersAPI.endpoints.getUsers.initiate(1));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.url).toBe('http://localhost:5173/api/users/GetClientUsers?page=1');
    });

    it('deleteUsers sends a DELETE with the ids in the body', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: true }));
        const store = makeStore();

        await store.dispatch(usersAPI.endpoints.deleteUsers.initiate(['u1']));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.method).toBe('DELETE');
        await expect(request.clone().json()).resolves.toEqual(['u1']);
    });

    it('getUserById requests the correct URL', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: { id: 'u1' } }));
        const store = makeStore();

        await store.dispatch(usersAPI.endpoints.getUserById.initiate('u1'));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.url).toBe('http://localhost:5173/api/users/GetClientUserById?id=u1');
    });

    it('updateUser posts the user body', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse(200, { isSuccess: true, value: true }));
        const store = makeStore();
        const user = { id: 'u1', userMail: 'a@test.com', userName: 'alice' };

        await store.dispatch(usersAPI.endpoints.updateUser.initiate(user));

        const request = fetchMock.mock.calls[0][0] as Request;
        expect(request.method).toBe('POST');
        expect(request.url).toBe('http://localhost:5173/api/users/UpdateUser');
        await expect(request.clone().json()).resolves.toEqual(user);
    });
});
