import { describe, expect, it, vi } from 'vitest';
import { renderHook } from '@testing-library/react';

const mockUseSearchParams = vi.fn();
vi.mock('react-router', () => ({
    useSearchParams: () => mockUseSearchParams(),
}));

const mockUseGetPaymentByIdQuery = vi.fn();
vi.mock('../store/services/payments', () => ({
    useGetPaymentByIdQuery: (...args: unknown[]) => mockUseGetPaymentByIdQuery(...args),
}));

import { usePayment } from './usePayment';

describe('usePayment', () => {
    it('skips the query and returns null when no id is present', () => {
        mockUseSearchParams.mockReturnValue([new URLSearchParams('')]);
        mockUseGetPaymentByIdQuery.mockReturnValue({ data: undefined, isLoading: false, isError: false });

        const { result } = renderHook(() => usePayment());

        expect(mockUseGetPaymentByIdQuery).toHaveBeenCalledWith(0, { skip: true });
        expect(result.current.paymentModel).toBeNull();
    });

    it('returns the payment model when the query succeeds', () => {
        mockUseSearchParams.mockReturnValue([new URLSearchParams('id=5')]);
        const payment = { id: 5, userEmail: 'a@test.com' };
        mockUseGetPaymentByIdQuery.mockReturnValue({ data: { isSuccess: true, value: payment }, isLoading: false, isError: false });

        const { result } = renderHook(() => usePayment());

        expect(mockUseGetPaymentByIdQuery).toHaveBeenCalledWith(5, { skip: false });
        expect(result.current.paymentModel).toBe(payment);
    });

    it('returns null when the query result is unsuccessful', () => {
        mockUseSearchParams.mockReturnValue([new URLSearchParams('id=5')]);
        mockUseGetPaymentByIdQuery.mockReturnValue({ data: { isSuccess: false }, isLoading: false, isError: true });

        const { result } = renderHook(() => usePayment());

        expect(result.current.paymentModel).toBeNull();
        expect(result.current.isError).toBe(true);
    });
});
