import { describe, expect, it, vi, beforeEach } from 'vitest';
import { act, renderHook } from '@testing-library/react';

const mockNavigate = vi.fn();
vi.mock('react-router', () => ({
    useNavigate: () => mockNavigate,
}));

const mockToastError = vi.fn();
vi.mock('react-hot-toast', () => ({
    toast: { error: (...args: unknown[]) => mockToastError(...args) },
}));

const mockUseGetPaymentsQuery = vi.fn();
const mockRemovePayments = vi.fn();
const mockUseRemovePaymentsMutation = vi.fn();
vi.mock('../store/services/payments', () => ({
    useGetPaymentsQuery: (...args: unknown[]) => mockUseGetPaymentsQuery(...args),
    useRemovePaymentsMutation: () => mockUseRemovePaymentsMutation(),
}));

import { usePayments } from './usePayments';

const payment = { id: 1, userId: 'u1', amount: 12.345, paymentDate: '2024-01-01T00:00:00Z', status: 1, transactionId: 'TRK1' };

describe('usePayments', () => {
    beforeEach(() => {
        mockNavigate.mockReset();
        mockToastError.mockReset();
        mockRemovePayments.mockReset();
        mockUseRemovePaymentsMutation.mockReturnValue([mockRemovePayments]);
        mockUseGetPaymentsQuery.mockReturnValue({ data: { isSuccess: true, value: [payment] }, isLoading: false, isError: false });
    });

    it('maps successful query results into grid rows', () => {
        const { result } = renderHook(() => usePayments());

        expect(result.current.rows).toEqual([
            { id: 1, userId: 'u1', amount: '12.35', paymentDate: new Date(payment.paymentDate).toLocaleDateString(), status: 'Completed', transactionId: 'TRK1' },
        ]);
    });

    it('leaves rows empty when the query is unsuccessful', () => {
        mockUseGetPaymentsQuery.mockReturnValue({ data: { isSuccess: false }, isLoading: false, isError: false });
        const { result } = renderHook(() => usePayments());

        expect(result.current.rows).toEqual([]);
    });

    it('handleRowDoubleClick navigates to the payment page', () => {
        const { result } = renderHook(() => usePayments());

        act(() => {
            result.current.handleRowDoubleClick({ row: { id: 1 } } as never);
        });

        expect(mockNavigate).toHaveBeenCalledWith('/payment?id=1');
    });

    it('handleRowSelectionChange tracks explicit and "exclude" selections', () => {
        const { result } = renderHook(() => usePayments());

        act(() => {
            result.current.handleRowSelectionChange({ type: 'include', ids: new Set([1]) } as never);
        });
        expect(result.current.allSelected).toBe(false);
        expect(result.current.hasSelection).toBe(true);

        act(() => {
            result.current.handleRowSelectionChange({ type: 'exclude', ids: new Set() } as never);
        });
        expect(result.current.allSelected).toBe(true);
        expect(result.current.hasSelection).toBe(true);
    });

    it('handleDeleteSelected does nothing without a selection', async () => {
        const { result } = renderHook(() => usePayments());

        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockRemovePayments).not.toHaveBeenCalled();
    });

    it('handleDeleteSelected removes all rows when allSelected is true', async () => {
        mockRemovePayments.mockReturnValue({ unwrap: () => Promise.resolve() });
        const { result } = renderHook(() => usePayments());

        act(() => {
            result.current.handleRowSelectionChange({ type: 'exclude', ids: new Set() } as never);
        });
        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockRemovePayments).toHaveBeenCalledWith([1]);
        expect(result.current.allSelected).toBe(false);
    });

    it('handleDeleteSelected removes the explicitly selected ids', async () => {
        mockRemovePayments.mockReturnValue({ unwrap: () => Promise.resolve() });
        const { result } = renderHook(() => usePayments());

        act(() => {
            result.current.handleRowSelectionChange({ type: 'include', ids: new Set([1, 2]) } as never);
        });
        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockRemovePayments).toHaveBeenCalledWith([1, 2]);
    });

    it('handleDeleteSelected shows a toast on failure', async () => {
        mockRemovePayments.mockReturnValue({ unwrap: () => Promise.reject({ status: 500, data: {} }) });
        const { result } = renderHook(() => usePayments());

        act(() => {
            result.current.handleRowSelectionChange({ type: 'include', ids: new Set([1]) } as never);
        });
        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockToastError).toHaveBeenCalledTimes(1);
    });
});
