import { fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { getAccessToken } from '../../services/oidcUserManager';

const apiHost: string = import.meta.env.VITE_API_URL ?? '';
export const baseURL: string = apiHost ? `${apiHost}/api` : '/api';

export const baseQueryWithReauth = fetchBaseQuery({
    baseUrl: baseURL,
    prepareHeaders: async (headers) => {
        const token = await getAccessToken();
        if (token) {
            headers.set('Authorization', `Bearer ${token}`);
        }
        return headers;
    },
});
