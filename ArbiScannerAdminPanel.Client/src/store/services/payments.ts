import { createApi } from '@reduxjs/toolkit/query/react'
import type { PaymentModel, PaymentResultDTO } from '../../types/accountType'
import type { FluentResult } from '../../types/FluentResultType'
import { baseQueryWithReauth } from './baseQuery'

export const paymentsAPI = createApi({
    reducerPath: 'paymentsAPI',
    tagTypes: ['Payments'],
    baseQuery: baseQueryWithReauth,
    keepUnusedDataFor: 0,
    endpoints: (builder) => ({
        getPayments: builder.query<FluentResult<PaymentModel[]>, number>({
            query: (page) => `/payments/GetAllPayments?page=${page}`,
            providesTags: () => [{ type: 'Payments', id: 'LIST' }],
        }),
        removePayments: builder.mutation<FluentResult<boolean>, number[]>({
            query: (ids) => ({
                url: `/payments/RemovePayments`,
                method: 'DELETE',
                body: ids
            }), invalidatesTags: [{ type: 'Payments', id: 'LIST' }],
        }),
        getPaymentById: builder.query<FluentResult<PaymentResultDTO>, number>({
            query: (id) => `/payments/GetPaymentById?id=${id}`,
        }),
    }),
})

export const { useGetPaymentsQuery, useRemovePaymentsMutation, useGetPaymentByIdQuery } = paymentsAPI