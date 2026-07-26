import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

const mockUseCreateUserSubscription = vi.fn();
vi.mock('../../hooks/useCreateUserSubscription', () => ({
    useCreateUserSubscription: () => mockUseCreateUserSubscription(),
}));

import CreateUserSubscriptionPage from './CreateUserSubscriptionPage';

const baseReturn = {
    email: '', subscriptionId: null, selectedUser: null, userSearchResults: [], showUserDropdown: false,
    isSearching: false, subscriptions: [{ id: 1, type: 'Basic', price: 10, durationInDays: 30 }],
    subscriptionsLoading: false, isCreating: false,
    setSubscriptionId: vi.fn(), handleEmailChange: vi.fn(), handleUserSelect: vi.fn(), handleCreate: vi.fn(), handleCancel: vi.fn(),
};

describe('CreateUserSubscriptionPage', () => {
    it('renders the subscription options and disables Create without a selection', () => {
        mockUseCreateUserSubscription.mockReturnValue(baseReturn);
        render(<CreateUserSubscriptionPage />);

        expect(screen.getByText('Basic - $10.00')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Create' })).toBeDisabled();
    });

    it('shows the search spinner and dropdown results, and selects a user', async () => {
        const user = userEvent.setup();
        const handleUserSelect = vi.fn();
        mockUseCreateUserSubscription.mockReturnValue({
            ...baseReturn,
            email: 'ali',
            isSearching: true,
            showUserDropdown: true,
            userSearchResults: [{ id: 'u1', userMail: 'alice@test.com', isActiveSubscription: false }],
            handleUserSelect,
        });
        render(<CreateUserSubscriptionPage />);

        await user.click(screen.getByText('alice@test.com'));

        expect(handleUserSelect).toHaveBeenCalledWith({ id: 'u1', userMail: 'alice@test.com', isActiveSubscription: false });
    });

    it('shows the selected user confirmation and enables Create', async () => {
        const user = userEvent.setup();
        const handleCreate = vi.fn();
        mockUseCreateUserSubscription.mockReturnValue({
            ...baseReturn,
            selectedUser: { id: 'u1', userMail: 'alice@test.com', isActiveSubscription: false },
            subscriptionId: 1,
            handleCreate,
        });
        render(<CreateUserSubscriptionPage />);

        expect(screen.getByText('✓ User selected: alice@test.com')).toBeInTheDocument();
        await user.click(screen.getByRole('button', { name: 'Create' }));

        expect(handleCreate).toHaveBeenCalledTimes(1);
    });

    it('shows "Creating..." while the mutation is in flight', () => {
        mockUseCreateUserSubscription.mockReturnValue({ ...baseReturn, isCreating: true });
        render(<CreateUserSubscriptionPage />);

        expect(screen.getByText('Creating...')).toBeInTheDocument();
    });

    it('invokes handleCancel from the cancel button', async () => {
        const user = userEvent.setup();
        const handleCancel = vi.fn();
        mockUseCreateUserSubscription.mockReturnValue({ ...baseReturn, handleCancel });
        render(<CreateUserSubscriptionPage />);

        await user.click(screen.getByRole('button', { name: 'Cancel' }));

        expect(handleCancel).toHaveBeenCalledTimes(1);
    });

    it('calls handleEmailChange and setSubscriptionId on input', async () => {
        const user = userEvent.setup();
        const handleEmailChange = vi.fn();
        const setSubscriptionId = vi.fn();
        mockUseCreateUserSubscription.mockReturnValue({ ...baseReturn, handleEmailChange, setSubscriptionId });
        render(<CreateUserSubscriptionPage />);

        await user.type(screen.getByPlaceholderText('Search user by email...'), 'a');
        expect(handleEmailChange).toHaveBeenCalled();

        await user.selectOptions(screen.getByRole('combobox'), '1');
        expect(setSubscriptionId).toHaveBeenCalledWith(1);
    });
});
