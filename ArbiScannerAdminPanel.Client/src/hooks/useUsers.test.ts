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

const mockUseGetUsersQuery = vi.fn();
const mockDeleteUsers = vi.fn();
const mockUseDeleteUsersMutation = vi.fn();
vi.mock('../store/services/users', () => ({
    useGetUsersQuery: (...args: unknown[]) => mockUseGetUsersQuery(...args),
    useDeleteUsersMutation: () => mockUseDeleteUsersMutation(),
}));

import { useUsers } from './useUsers';

const userWithDates = { id: 'u1', userMail: 'a@test.com', isActiveSubscription: true, subscriptionStartDate: '2024-01-01', subscriptionEndDate: '2024-02-01' };
const userWithoutDates = { id: 'u2', userMail: 'b@test.com', isActiveSubscription: false, subscriptionStartDate: null, subscriptionEndDate: null };

describe('useUsers', () => {
    beforeEach(() => {
        mockNavigate.mockReset();
        mockToastError.mockReset();
        mockDeleteUsers.mockReset();
        mockUseDeleteUsersMutation.mockReturnValue([mockDeleteUsers]);
        mockUseGetUsersQuery.mockReturnValue({ data: { isSuccess: true, value: [userWithDates, userWithoutDates] }, isLoading: false, isError: false });
    });

    it('maps successful query results into grid rows, formatting or defaulting dates', () => {
        const { result } = renderHook(() => useUsers());

        expect(result.current.rows).toEqual([
            {
                id: 'u1', userMail: 'a@test.com', isActiveSubscription: true,
                subscriptionStartDate: new Date('2024-01-01').toLocaleDateString(),
                subscriptionEndDate: new Date('2024-02-01').toLocaleDateString(),
            },
            { id: 'u2', userMail: 'b@test.com', isActiveSubscription: false, subscriptionStartDate: 'N/A', subscriptionEndDate: 'N/A' },
        ]);
    });

    it('handleRowDoubleClick navigates to the user page', () => {
        const { result } = renderHook(() => useUsers());

        act(() => {
            result.current.handleRowDoubleClick({ row: { id: 'u1' } } as never);
        });

        expect(mockNavigate).toHaveBeenCalledWith('/user?id=u1');
    });

    it('handleDeleteSelected removes all rows when allSelected is true', async () => {
        mockDeleteUsers.mockReturnValue({ unwrap: () => Promise.resolve() });
        const { result } = renderHook(() => useUsers());

        act(() => {
            result.current.handleRowSelectionChange({ type: 'exclude', ids: new Set() } as never);
        });
        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockDeleteUsers).toHaveBeenCalledWith(['u1', 'u2']);
    });

    it('handleDeleteSelected removes explicitly selected ids', async () => {
        mockDeleteUsers.mockReturnValue({ unwrap: () => Promise.resolve() });
        const { result } = renderHook(() => useUsers());

        act(() => {
            result.current.handleRowSelectionChange({ type: 'include', ids: new Set(['u1']) } as never);
        });
        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockDeleteUsers).toHaveBeenCalledWith(['u1']);
    });

    it('handleDeleteSelected shows a toast on failure', async () => {
        mockDeleteUsers.mockReturnValue({ unwrap: () => Promise.reject({ status: 500, data: {} }) });
        const { result } = renderHook(() => useUsers());

        act(() => {
            result.current.handleRowSelectionChange({ type: 'include', ids: new Set(['u1']) } as never);
        });
        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockToastError).toHaveBeenCalledTimes(1);
    });

    it('handleDeleteSelected does nothing without a selection', async () => {
        const { result } = renderHook(() => useUsers());

        await act(async () => {
            await result.current.handleDeleteSelected();
        });

        expect(mockDeleteUsers).not.toHaveBeenCalled();
    });
});
