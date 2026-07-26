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

const mockUseGetUserSubscriptionsQuery = vi.fn();
const mockDeleteUserSubscriptions = vi.fn();
const mockUseDeleteUserSubscriptionsMutation = vi.fn();
vi.mock('../store/services/userSubscriptions', () => ({
    useGetUserSubscriptionsQuery: (...args: unknown[]) => mockUseGetUserSubscriptionsQuery(...args),
    useDeleteUserSubscriptionsMutation: () => mockUseDeleteUserSubscriptionsMutation(),
}));

import { useUserSubscriptions } from './useUserSubscriptions';

const row = { id: 1, userMail: 'a@test.com', subcriptionType: 'Basic', subscriptionStartDate: '2024-01-01', subscriptionEndDate: '2024-02-01' };

describe('useUserSubscriptions', () => {
    beforeEach(() => {
        mockNavigate.mockReset();
        mockToastError.mockReset();
        mockDeleteUserSubscriptions.mockReset();
        mockUseDeleteUserSubscriptionsMutation.mockReturnValue([mockDeleteUserSubscriptions]);
        mockUseGetUserSubscriptionsQuery.mockReturnValue({ data: { isSuccess: true, value: [row] }, isLoading: false, isError: false });
    });

    it('maps successful query results into grid rows', () => {
        const { result } = renderHook(() => useUserSubscriptions());

        expect(result.current.rows).toEqual([row]);
    });

    it('handleCreate navigates to the create page', () => {
        const { result } = renderHook(() => useUserSubscriptions());

        act(() => {
            result.current.handleCreate();
        });

        expect(mockNavigate).toHaveBeenCalledWith('/createusersubscription');
    });

    it('handleRowDoubleClick navigates to the detail page', () => {
        const { result } = renderHook(() => useUserSubscriptions());

        act(() => {
            result.current.handleRowDoubleClick({ row: { id: 1 } } as never);
        });

        expect(mockNavigate).toHaveBeenCalledWith('/usersubscription?id=1');
    });

    it('handleDeleteSelected removes all rows when allSelected is true', async () => {
        mockDeleteUserSubscriptions.mockReturnValue({ unwrap: () => Promise.resolve() });
        const { result } = renderHook(() => useUserSubscriptions());

        act(() => {
            result.current.handleRowSelectionChange({ type: 'exclude', ids: new Set() } as never);
        });
        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockDeleteUserSubscriptions).toHaveBeenCalledWith([1]);
    });

    it('handleDeleteSelected removes explicitly selected ids', async () => {
        mockDeleteUserSubscriptions.mockReturnValue({ unwrap: () => Promise.resolve() });
        const { result } = renderHook(() => useUserSubscriptions());

        act(() => {
            result.current.handleRowSelectionChange({ type: 'include', ids: new Set([1]) } as never);
        });
        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockDeleteUserSubscriptions).toHaveBeenCalledWith([1]);
    });

    it('handleDeleteSelected shows a toast on failure', async () => {
        mockDeleteUserSubscriptions.mockReturnValue({ unwrap: () => Promise.reject({ status: 500, data: {} }) });
        const { result } = renderHook(() => useUserSubscriptions());

        act(() => {
            result.current.handleRowSelectionChange({ type: 'include', ids: new Set([1]) } as never);
        });
        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockToastError).toHaveBeenCalledTimes(1);
    });

    it('handleDeleteSelected does nothing without a selection', async () => {
        const { result } = renderHook(() => useUserSubscriptions());

        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockDeleteUserSubscriptions).not.toHaveBeenCalled();
    });
});
