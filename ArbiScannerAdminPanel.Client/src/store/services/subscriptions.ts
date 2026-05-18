import { createApi } from '@reduxjs/toolkit/query/react'
import type { SubscriptionModel } from '../../types/accountType'
import type { FluentResult } from '../../types/FluentResultType'
import { baseQueryWithReauth } from './baseQuery'

export const subscriptionsAPI = createApi({
    reducerPath: 'subscriptionsAPI',
    tagTypes: ['Subscriptions'],
    baseQuery: baseQueryWithReauth,
    keepUnusedDataFor: 0,
    endpoints: (builder) => ({
        getSubscriptions: builder.query<FluentResult<SubscriptionModel[]>, number>({
            query: (page) => `/subscriptions/GetAllSubscriptions?page=${page}`,
            providesTags: () => [{ type: 'Subscriptions', id: 'LIST' }],
        }),
        deleteSubscriptions: builder.mutation<FluentResult<boolean>, number[]>({
            query: (ids) => ({
                url: `/subscriptions/DeleteSubscriptionsById`,
                method: 'DELETE',
                body: ids
            }), invalidatesTags: [{ type: 'Subscriptions', id: 'LIST' }],
        }),
        getSubscriptionById: builder.query<FluentResult<SubscriptionModel>, number>({
            query: (id) => `/subscriptions/GetSubscriptionById?id=${id}`,
        }),
        updateSubscription: builder.mutation<FluentResult<boolean>, SubscriptionModel>({
            query: (subscription) => ({
                url: `/subscriptions/UpdateSubscription`,
                method: 'POST',
                body: subscription
            }), invalidatesTags: [{ type: 'Subscriptions', id: 'LIST' }],
        }),
        createSubscription: builder.mutation<FluentResult<SubscriptionModel>, SubscriptionModel>({
            query: (subscription) => ({
                url: `/subscriptions/CreateSubscription`,
                method: 'POST',
                body: subscription
            }), invalidatesTags: [{ type: 'Subscriptions', id: 'LIST' }],
        }),
    }),
})

export const { useGetSubscriptionsQuery, useDeleteSubscriptionsMutation, useGetSubscriptionByIdQuery, useCreateSubscriptionMutation, useUpdateSubscriptionMutation } = subscriptionsAPI