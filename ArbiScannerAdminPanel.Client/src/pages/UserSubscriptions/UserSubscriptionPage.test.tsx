import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

const mockUseUserSubscription = vi.fn();
vi.mock('../../hooks/useUserSubscription', () => ({
    useUserSubscription: () => mockUseUserSubscription(),
}));

import UserSubscriptionPage from './UserSubscriptionPage';

const model = {
    id: 1, userId: 'u1', subscriptionId: 1,
    subscription: { id: 1, type: 'Basic', price: 10, durationInDays: 30 },
    startDate: '2024-01-01T00:00:00Z', endDate: '2024-02-01T00:00:00Z',
};

describe('UserSubscriptionPage', () => {
    it('shows a loading state', () => {
        mockUseUserSubscription.mockReturnValue({ userSubscriptionModel: model, isLoading: true, isError: false });
        render(<UserSubscriptionPage />);

        expect(screen.getByText('Loading...')).toBeInTheDocument();
    });

    it('shows an error state', () => {
        mockUseUserSubscription.mockReturnValue({ userSubscriptionModel: model, isLoading: false, isError: true });
        render(<UserSubscriptionPage />);

        expect(screen.getByText('Failed to load this subscription. Please try again later.')).toBeInTheDocument();
    });

    it('renders the read-only view with an Edit button', async () => {
        const user = userEvent.setup();
        const setIsEditMode = vi.fn();
        mockUseUserSubscription.mockReturnValue({
            userSubscriptionModel: model, isEditMode: false, isLoading: false, isError: false,
            isEndDateValid: () => true, setIsEditMode, handleInputChange: vi.fn(), handleSave: vi.fn(), handleCancel: vi.fn(),
        });
        render(<UserSubscriptionPage />);

        expect(screen.getByText('Basic')).toBeInTheDocument();
        expect(screen.getByText('30 days')).toBeInTheDocument();
        await user.click(screen.getByText('Edit'));

        expect(setIsEditMode).toHaveBeenCalledWith(true);
    });

    it('renders the edit form, disables Save for an invalid end date, and calls handlers', async () => {
        const user = userEvent.setup();
        const handleSave = vi.fn();
        const handleCancel = vi.fn();
        mockUseUserSubscription.mockReturnValue({
            userSubscriptionModel: model, isEditMode: true, isLoading: false, isError: false,
            isEndDateValid: () => false, setIsEditMode: vi.fn(), handleInputChange: vi.fn(), handleSave, handleCancel,
        });
        render(<UserSubscriptionPage />);

        expect(screen.getByText('End date cannot be in the past')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled();

        await user.click(screen.getByRole('button', { name: 'Cancel' }));
        expect(handleCancel).toHaveBeenCalledTimes(1);
        expect(handleSave).not.toHaveBeenCalled();
    });

    it('enables Save for a valid end date and calls handleSave', async () => {
        const user = userEvent.setup();
        const handleSave = vi.fn();
        mockUseUserSubscription.mockReturnValue({
            userSubscriptionModel: model, isEditMode: true, isLoading: false, isError: false,
            isEndDateValid: () => true, setIsEditMode: vi.fn(), handleInputChange: vi.fn(), handleSave, handleCancel: vi.fn(),
        });
        render(<UserSubscriptionPage />);

        const saveButton = screen.getByRole('button', { name: 'Save' });
        expect(saveButton).not.toBeDisabled();
        await user.click(saveButton);

        expect(handleSave).toHaveBeenCalledTimes(1);
    });
});
