import { createApi } from '@reduxjs/toolkit/query/react'
import type { UserSubscriptionCreateDTO, UserSubscriptionModel, UserSubscriptionRowDTO } from '../../types/accountType'
import type { FluentResult } from '../../types/FluentResultType'
import { baseQueryWithReauth } from './baseQuery'

export const userSubscriptionsAPI = createApi({
    reducerPath: 'userSubscriptionsAPI',
    tagTypes: ['UserSubscriptions'],
    baseQuery: baseQueryWithReauth,
    keepUnusedDataFor: 0,
    endpoints: (builder) => ({
        getUserSubscriptions: builder.query<FluentResult<UserSubscriptionRowDTO[]>, number>({
            query: (page) => `/subscriptions/GetAllUserSubscriptions?page=${page}`,
            providesTags: () => [{ type: 'UserSubscriptions', id: 'LIST' }],
        }),
        deleteUserSubscriptions: builder.mutation<FluentResult<boolean>, number[]>({
            query: (ids) => ({
                url: `/subscriptions/DeleteUserSubscriptionsById`,
                method: 'DELETE',
                body: ids
            }), invalidatesTags: [{ type: 'UserSubscriptions', id: 'LIST' }],
        }),
        getUserSubscriptionById: builder.query<FluentResult<UserSubscriptionModel>, number>({
            query: (id) => `/subscriptions/GetUserSubscriptionById?Id=${id}`,
        }),
        updateUserSubscription: builder.mutation<FluentResult<boolean>, UserSubscriptionModel>({
            query: (subscription) => ({
                url: `/subscriptions/UpdateUserSubscription`,
                method: 'POST',
                body: subscription
            }), invalidatesTags: [{ type: 'UserSubscriptions', id: 'LIST' }],
        }),
        createUserSubscription: builder.mutation<FluentResult<UserSubscriptionModel>, UserSubscriptionCreateDTO>({
            query: (subscription) => ({
                url: `/subscriptions/CreateUserSubscription`,
                method: 'POST',
                body: subscription
            }), invalidatesTags: [{ type: 'UserSubscriptions', id: 'LIST' }],
        }),
    }),
})

export const { useGetUserSubscriptionsQuery, useDeleteUserSubscriptionsMutation, useGetUserSubscriptionByIdQuery, useUpdateUserSubscriptionMutation, useCreateUserSubscriptionMutation } = userSubscriptionsAPI