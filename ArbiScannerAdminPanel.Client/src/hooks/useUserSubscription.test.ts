import { describe, expect, it, vi, beforeEach } from 'vitest';
import { act, renderHook } from '@testing-library/react';

const mockUseSearchParams = vi.fn();
const mockNavigate = vi.fn();
vi.mock('react-router', () => ({
    useSearchParams: () => mockUseSearchParams(),
    useNavigate: () => mockNavigate,
}));

const mockToastError = vi.fn();
vi.mock('react-hot-toast', () => ({
    toast: { error: (...args: unknown[]) => mockToastError(...args) },
}));

const mockUseGetUserSubscriptionByIdQuery = vi.fn();
const mockUpdate = vi.fn();
const mockUseUpdateUserSubscriptionMutation = vi.fn();
vi.mock('../store/services/userSubscriptions', () => ({
    useGetUserSubscriptionByIdQuery: (...args: unknown[]) => mockUseGetUserSubscriptionByIdQuery(...args),
    useUpdateUserSubscriptionMutation: () => mockUseUpdateUserSubscriptionMutation(),
}));

import { useUserSubscription } from './useUserSubscription';

const futureDate = new Date(Date.now() + 86400000).toISOString();
const pastDate = new Date(Date.now() - 86400000).toISOString();

describe('useUserSubscription', () => {
    beforeEach(() => {
        mockUseSearchParams.mockReturnValue([new URLSearchParams('id=5')]);
        mockUpdate.mockReset();
        mockUseUpdateUserSubscriptionMutation.mockReturnValue([mockUpdate]);
        mockNavigate.mockReset();
        mockToastError.mockReset();
    });

    it('populates the model from a successful query and exits edit mode', () => {
        const value = { id: 5, userId: 'u1', subscriptionId: 1, subscription: null, startDate: '2024-01-01', endDate: futureDate };
        mockUseGetUserSubscriptionByIdQuery.mockReturnValue({ data: { isSuccess: true, value }, isLoading: false, isError: false });

        const { result } = renderHook(() => useUserSubscription());

        expect(result.current.userSubscriptionModel).toEqual(value);
        expect(result.current.isEditMode).toBe(false);
    });

    it('handleInputChange updates a single field', () => {
        mockUseGetUserSubscriptionByIdQuery.mockReturnValue({ data: undefined, isLoading: false, isError: false });
        const { result } = renderHook(() => useUserSubscription());

        act(() => {
            result.current.handleInputChange({ target: { name: 'endDate', value: futureDate } } as React.ChangeEvent<HTMLInputElement>);
        });

        expect(result.current.userSubscriptionModel.endDate).toBe(futureDate);
    });

    it('isEndDateValid reflects whether the end date is in the future', () => {
        mockUseGetUserSubscriptionByIdQuery.mockReturnValue({ data: undefined, isLoading: false, isError: false });
        const { result } = renderHook(() => useUserSubscription());

        act(() => {
            result.current.handleInputChange({ target: { name: 'endDate', value: pastDate } } as React.ChangeEvent<HTMLInputElement>);
        });
        expect(result.current.isEndDateValid()).toBe(false);

        act(() => {
            result.current.handleInputChange({ target: { name: 'endDate', value: futureDate } } as React.ChangeEvent<HTMLInputElement>);
        });
        expect(result.current.isEndDateValid()).toBe(true);
    });

    it('handleSave does nothing when the end date is invalid', async () => {
        mockUseGetUserSubscriptionByIdQuery.mockReturnValue({ data: undefined, isLoading: false, isError: false });
        const { result } = renderHook(() => useUserSubscription());

        await act(async () => {
            await result.current.handleSave();
        });

        expect(mockUpdate).not.toHaveBeenCalled();
    });

    it('handleSave updates and navigates on success', async () => {
        const value = { id: 5, userId: 'u1', subscriptionId: 1, subscription: null, startDate: '2024-01-01', endDate: futureDate };
        mockUseGetUserSubscriptionByIdQuery.mockReturnValue({ data: { isSuccess: true, value }, isLoading: false, isError: false });
        mockUpdate.mockReturnValue({ unwrap: () => Promise.resolve() });

        const { result } = renderHook(() => useUserSubscription());

        await act(async () => {
            await result.current.handleSave();
        });

        expect(mockUpdate).toHaveBeenCalledWith(value);
        expect(mockNavigate).toHaveBeenCalledWith('/userSubscriptions');
    });

    it('handleSave shows a toast when the mutation fails', async () => {
        const value = { id: 5, userId: 'u1', subscriptionId: 1, subscription: null, startDate: '2024-01-01', endDate: futureDate };
        mockUseGetUserSubscriptionByIdQuery.mockReturnValue({ data: { isSuccess: true, value }, isLoading: false, isError: false });
        mockUpdate.mockReturnValue({ unwrap: () => Promise.reject({ status: 500, data: {} }) });

        const { result } = renderHook(() => useUserSubscription());

        await act(async () => {
            await result.current.handleSave();
        });

        expect(mockToastError).toHaveBeenCalledTimes(1);
        expect(mockNavigate).not.toHaveBeenCalled();
    });

    it('handleCancel resets edit mode and restores the fetched value', () => {
        const value = { id: 5, userId: 'u1', subscriptionId: 1, subscription: null, startDate: '2024-01-01', endDate: futureDate };
        mockUseGetUserSubscriptionByIdQuery.mockReturnValue({ data: { isSuccess: true, value }, isLoading: false, isError: false });
        const { result } = renderHook(() => useUserSubscription());

        act(() => {
            result.current.setIsEditMode(true);
        });
        act(() => {
            result.current.handleCancel();
        });

        expect(result.current.isEditMode).toBe(false);
        expect(result.current.userSubscriptionModel).toEqual(value);
    });
});
