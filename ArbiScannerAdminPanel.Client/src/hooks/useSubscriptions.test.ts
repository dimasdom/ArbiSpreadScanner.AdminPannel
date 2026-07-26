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

const mockUseGetSubscriptionsQuery = vi.fn();
const mockDeleteSubscriptions = vi.fn();
const mockUseDeleteSubscriptionsMutation = vi.fn();
vi.mock('../store/services/subscriptions', () => ({
    useGetSubscriptionsQuery: (...args: unknown[]) => mockUseGetSubscriptionsQuery(...args),
    useDeleteSubscriptionsMutation: () => mockUseDeleteSubscriptionsMutation(),
}));

import { useSubscriptions } from './useSubscriptions';

const subscription = { id: 1, type: 'Basic', price: 10, durationInDays: 30 };

describe('useSubscriptions', () => {
    beforeEach(() => {
        mockNavigate.mockReset();
        mockToastError.mockReset();
        mockDeleteSubscriptions.mockReset();
        mockUseDeleteSubscriptionsMutation.mockReturnValue([mockDeleteSubscriptions]);
        mockUseGetSubscriptionsQuery.mockReturnValue({ data: { isSuccess: true, value: [subscription] }, isLoading: false, isError: false });
    });

    it('maps successful query results into grid rows', () => {
        const { result } = renderHook(() => useSubscriptions());

        expect(result.current.rows).toEqual([{ id: 1, type: 'Basic', price: 10, durationInDays: 30 }]);
    });

    it('handleCreate navigates to the create subscription page', () => {
        const { result } = renderHook(() => useSubscriptions());

        act(() => {
            result.current.handleCreate();
        });

        expect(mockNavigate).toHaveBeenCalledWith('/subscription');
    });

    it('handleRowDoubleClick navigates to the subscription page', () => {
        const { result } = renderHook(() => useSubscriptions());

        act(() => {
            result.current.handleRowDoubleClick({ row: { id: 1 } } as never);
        });

        expect(mockNavigate).toHaveBeenCalledWith('/subscription?id=1');
    });

    it('handleDeleteSelected does nothing without a selection', async () => {
        const { result } = renderHook(() => useSubscriptions());

        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockDeleteSubscriptions).not.toHaveBeenCalled();
    });

    it('handleDeleteSelected removes all rows when allSelected is true', async () => {
        mockDeleteSubscriptions.mockReturnValue({ unwrap: () => Promise.resolve() });
        const { result } = renderHook(() => useSubscriptions());

        act(() => {
            result.current.handleRowSelectionChange({ type: 'exclude', ids: new Set() } as never);
        });
        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockDeleteSubscriptions).toHaveBeenCalledWith([1]);
    });

    it('handleDeleteSelected removes explicitly selected ids', async () => {
        mockDeleteSubscriptions.mockReturnValue({ unwrap: () => Promise.resolve() });
        const { result } = renderHook(() => useSubscriptions());

        act(() => {
            result.current.handleRowSelectionChange({ type: 'include', ids: new Set([1]) } as never);
        });
        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockDeleteSubscriptions).toHaveBeenCalledWith([1]);
    });

    it('handleDeleteSelected shows a toast on failure', async () => {
        mockDeleteSubscriptions.mockReturnValue({ unwrap: () => Promise.reject({ status: 500, data: {} }) });
        const { result } = renderHook(() => useSubscriptions());

        act(() => {
            result.current.handleRowSelectionChange({ type: 'include', ids: new Set([1]) } as never);
        });
        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockToastError).toHaveBeenCalledTimes(1);
    });
});
