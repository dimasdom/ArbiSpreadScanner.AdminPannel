import { createApi } from '@reduxjs/toolkit/query/react'
import type { ClientAccountDTO, ClientAccountTableRowDTO } from '../../types/accountType'
import type { FluentResult } from '../../types/FluentResultType'
import { baseQueryWithReauth } from './baseQuery'

export const usersAPI = createApi({
    reducerPath: 'usersAPI',
    tagTypes: ['Users'],
    baseQuery: baseQueryWithReauth,
    keepUnusedDataFor: 0,
    endpoints: (builder) => ({
        getUsers: builder.query<FluentResult<ClientAccountTableRowDTO[]>, number>({
            query: (page) => `/users/GetClientUsers?page=${page}`,
            providesTags: () => [{ type: 'Users', id: 'LIST' }],
        }),
        deleteUsers: builder.mutation<FluentResult<boolean>, string[]>({
            query: (ids) => ({
                url: `/users/DeleteClientUsers`,
                method: 'DELETE',
                body: ids
            }), invalidatesTags: [{ type: 'Users', id: 'LIST' }],
        }),
        getUserById: builder.query<FluentResult<ClientAccountDTO>, string>({
            query: (id) => `/users/GetClientUserById?id=${id}`,
        }),
        updateUser: builder.mutation<FluentResult<boolean>, ClientAccountDTO>({
            query: (user) => ({
                url: `/users/UpdateUser`,
                method: 'POST',
                body: user
            }), invalidatesTags: [{ type: 'Users', id: 'LIST' }],
        }),
    }),
})

export const { useGetUsersQuery, useDeleteUsersMutation, useGetUserByIdQuery } = usersAPI