import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { act, renderHook, waitFor } from '@testing-library/react';

const mockNavigate = vi.fn();
vi.mock('react-router', () => ({
    useNavigate: () => mockNavigate,
}));

const mockToastError = vi.fn();
vi.mock('react-hot-toast', () => ({
    toast: { error: (...args: unknown[]) => mockToastError(...args) },
}));

const mockUseGetSubscriptionsQuery = vi.fn();
vi.mock('../store/services/subscriptions', () => ({
    useGetSubscriptionsQuery: (...args: unknown[]) => mockUseGetSubscriptionsQuery(...args),
}));

const mockUseGetUsersQuery = vi.fn();
vi.mock('../store/services/users', () => ({
    useGetUsersQuery: (...args: unknown[]) => mockUseGetUsersQuery(...args),
}));

const mockCreate = vi.fn();
const mockUseCreateUserSubscriptionMutation = vi.fn();
vi.mock('../store/services/userSubscriptions', () => ({
    useCreateUserSubscriptionMutation: () => mockUseCreateUserSubscriptionMutation(),
}));

import { useCreateUserSubscription } from './useCreateUserSubscription';

const users = [
    { id: 'u1', userMail: 'alice@test.com', isActiveSubscription: false },
    { id: 'u2', userMail: 'bob@test.com', isActiveSubscription: false },
];

describe('useCreateUserSubscription', () => {
    beforeEach(() => {
        vi.useFakeTimers();
        mockNavigate.mockReset();
        mockToastError.mockReset();
        mockCreate.mockReset();
        mockUseCreateUserSubscriptionMutation.mockReturnValue([mockCreate, { isLoading: false }]);
        mockUseGetSubscriptionsQuery.mockReturnValue({ data: { isSuccess: true, value: [{ id: 1, type: 'Basic', price: 10, durationInDays: 30 }] }, isLoading: false });
        mockUseGetUsersQuery.mockReturnValue({ data: { isSuccess: true, value: users } });
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('returns empty subscriptions when the query is unsuccessful', () => {
        mockUseGetSubscriptionsQuery.mockReturnValue({ data: { isSuccess: false }, isLoading: false });
        const { result } = renderHook(() => useCreateUserSubscription());

        expect(result.current.subscriptions).toEqual([]);
    });

    it('filters users by email after the debounce timer', () => {
        const { result } = renderHook(() => useCreateUserSubscription());

        act(() => {
            result.current.handleEmailChange('alice');
        });
        act(() => {
            vi.advanceTimersByTime(300);
        });

        expect(result.current.userSearchResults).toEqual([users[0]]);
        expect(result.current.showUserDropdown).toBe(true);
    });

    it('clears search results when email is blank', () => {
        const { result } = renderHook(() => useCreateUserSubscription());

        act(() => {
            result.current.handleEmailChange('');
        });
        act(() => {
            vi.advanceTimersByTime(300);
        });

        expect(result.current.userSearchResults).toEqual([]);
        expect(result.current.showUserDropdown).toBe(false);
    });

    it('handleUserSelect sets the selected user and closes the dropdown', () => {
        const { result } = renderHook(() => useCreateUserSubscription());

        act(() => {
            result.current.handleUserSelect(users[0]);
        });

        expect(result.current.selectedUser).toEqual(users[0]);
        expect(result.current.email).toBe('alice@test.com');
        expect(result.current.showUserDropdown).toBe(false);
    });

    it('handleCreate does nothing without a selected user or subscription', async () => {
        const { result } = renderHook(() => useCreateUserSubscription());

        await act(async () => {
            await result.current.handleCreate();
        });

        expect(mockCreate).not.toHaveBeenCalled();
    });

    it('handleCreate creates the subscription and navigates on success', async () => {
        mockCreate.mockReturnValue({ unwrap: () => Promise.resolve() });
        vi.useRealTimers();
        const { result } = renderHook(() => useCreateUserSubscription());

        act(() => {
            result.current.handleUserSelect(users[0]);
        });
        act(() => {
            result.current.setSubscriptionId(1);
        });

        await act(async () => {
            await result.current.handleCreate();
        });

        expect(mockCreate).toHaveBeenCalledWith({ userEmail: 'alice@test.com', subscriptionId: 1 });
        expect(mockNavigate).toHaveBeenCalledWith('/userSubscriptions');
    });

    it('handleCreate shows a toast when the mutation fails', async () => {
        mockCreate.mockReturnValue({ unwrap: () => Promise.reject({ status: 500, data: {} }) });
        vi.useRealTimers();
        const { result } = renderHook(() => useCreateUserSubscription());

        act(() => {
            result.current.handleUserSelect(users[0]);
        });
        act(() => {
            result.current.setSubscriptionId(1);
        });

        await act(async () => {
            await result.current.handleCreate();
        });

        await waitFor(() => expect(mockToastError).toHaveBeenCalledTimes(1));
    });

    it('handleCancel navigates back to the user subscriptions list', () => {
        const { result } = renderHook(() => useCreateUserSubscription());

        act(() => {
            result.current.handleCancel();
        });

        expect(mockNavigate).toHaveBeenCalledWith('/userSubscriptions');
    });
});
