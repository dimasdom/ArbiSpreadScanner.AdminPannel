import { createApi } from '@reduxjs/toolkit/query/react';
import type { AdminAccountDTO, AdminAccountAuthenticateDTO } from '../../types/accountType';
import type { FluentResult } from '../../types/FluentResultType';
import {
    setAuthenticated,
    setError,
    setLoading,
    clearError,
    logout,
    markSessionChecked,
} from '../slices/accountSlice';
import { baseQueryWithReauth } from './baseQuery';

const getResultMessage = (result: FluentResult<unknown> | undefined, fallback: string) =>
    result?.errors?.[0]?.message || result?.reasons?.[0]?.message || fallback;

const getErrorMessage = (error: unknown, fallback: string) => {
    if (typeof error === 'object' && error !== null && 'error' in error) {
        const nestedError = (error as { error?: unknown }).error;
        if (typeof nestedError === 'string') return nestedError;
    }
    return fallback;
};

export const accountApi = createApi({
    reducerPath: 'accountApi',
    baseQuery: baseQueryWithReauth,
    tagTypes: ['Account'],
    endpoints: (builder) => ({
        login: builder.mutation<FluentResult<AdminAccountDTO>, AdminAccountAuthenticateDTO>({
            query: (payload) => ({
                url: '/Account/Authenticate',
                method: 'POST',
                body: payload,
            }),
            async onQueryStarted(_, { dispatch, queryFulfilled }) {
                dispatch(setLoading(true));
                dispatch(clearError());
                try {
                    const { data } = await queryFulfilled;
                    if (!data.isSuccess) {
                        dispatch(setError(getResultMessage(data, 'Login failed')));
                        dispatch(setLoading(false));
                        return;
                    }
                    dispatch(setAuthenticated());
                } catch (error) {
                    dispatch(setError(getErrorMessage(error, 'Login failed')));
                    dispatch(setLoading(false));
                }
            },
        }),

        me: builder.query<FluentResult<AdminAccountDTO>, void>({
            query: () => ({ url: '/Account/Me', method: 'GET' }),
            providesTags: ['Account'],
            async onQueryStarted(_, { dispatch, queryFulfilled }) {
                try {
                    const { data } = await queryFulfilled;
                    if (data.isSuccess) {
                        dispatch(setAuthenticated());
                    } else {
                        dispatch(logout());
                    }
                } catch {
                    dispatch(logout());
                } finally {
                    dispatch(markSessionChecked());
                }
            },
        }),

        logout: builder.mutation<void, void>({
            query: () => ({
                url: '/Account/Logout',
                method: 'POST',
                body: {},
            }),
            async onQueryStarted(_, { dispatch, queryFulfilled }) {
                try {
                    await queryFulfilled;
                } finally {
                    dispatch(logout());
                }
            },
        }),

        refresh: builder.mutation<void, void>({
            query: () => ({
                url: '/Account/Refresh',
                method: 'POST',
                body: {},
            }),
        }),
    }),
});

export const {
    useLoginMutation,
    useMeQuery,
    useLogoutMutation,
    useRefreshMutation,
} = accountApi;
