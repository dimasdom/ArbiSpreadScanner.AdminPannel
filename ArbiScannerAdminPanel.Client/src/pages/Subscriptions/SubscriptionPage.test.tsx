import { describe, expect, it, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

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

const mockUseGetSubscriptionByIdQuery = vi.fn();
const mockCreateSubscription = vi.fn();
const mockUpdateSubscription = vi.fn();
vi.mock('../../store/services/subscriptions', () => ({
    useGetSubscriptionByIdQuery: (...args: unknown[]) => mockUseGetSubscriptionByIdQuery(...args),
    useCreateSubscriptionMutation: () => [mockCreateSubscription],
    useUpdateSubscriptionMutation: () => [mockUpdateSubscription],
}));

import SubscriptionPage from './SubscriptionPage';

describe('SubscriptionPage', () => {
    beforeEach(() => {
        mockNavigate.mockReset();
        mockToastError.mockReset();
        mockCreateSubscription.mockReset();
        mockUpdateSubscription.mockReset();
    });

    it('starts in create mode with an empty editable form when there is no id', () => {
        mockUseSearchParams.mockReturnValue([new URLSearchParams('')]);
        mockUseGetSubscriptionByIdQuery.mockReturnValue({ data: undefined, isLoading: false, isError: false });

        render(<SubscriptionPage />);

        expect(screen.getByText('Create Subscription')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
    });

    it('shows a loading state in edit mode', () => {
        mockUseSearchParams.mockReturnValue([new URLSearchParams('id=1')]);
        mockUseGetSubscriptionByIdQuery.mockReturnValue({ data: undefined, isLoading: true, isError: false });

        render(<SubscriptionPage />);

        expect(screen.getByText('Loading...')).toBeInTheDocument();
    });

    it('shows an error state in edit mode', () => {
        mockUseSearchParams.mockReturnValue([new URLSearchParams('id=1')]);
        mockUseGetSubscriptionByIdQuery.mockReturnValue({ data: undefined, isLoading: false, isError: true });

        render(<SubscriptionPage />);

        expect(screen.getByText('Failed to load this subscription. Please try again later.')).toBeInTheDocument();
    });

    it('renders the read-only view once loaded, and toggles into edit mode', async () => {
        const user = userEvent.setup();
        mockUseSearchParams.mockReturnValue([new URLSearchParams('id=1')]);
        mockUseGetSubscriptionByIdQuery.mockReturnValue({
            data: { isSuccess: true, value: { id: 1, type: 'Basic', price: 10, durationInDays: 30 } },
            isLoading: false, isError: false,
        });

        render(<SubscriptionPage />);

        expect(screen.getByText('Subscription Details')).toBeInTheDocument();
        expect(screen.getByText('$10.00')).toBeInTheDocument();

        await user.click(screen.getByText('Edit'));
        expect(screen.getByDisplayValue('Basic')).toBeInTheDocument();
    });

    it('creates a new subscription and navigates on save', async () => {
        const user = userEvent.setup();
        mockUseSearchParams.mockReturnValue([new URLSearchParams('')]);
        mockUseGetSubscriptionByIdQuery.mockReturnValue({ data: undefined, isLoading: false, isError: false });
        mockCreateSubscription.mockReturnValue({ unwrap: () => Promise.resolve() });

        render(<SubscriptionPage />);

        await user.type(screen.getByPlaceholderText('e.g., Basic, Standard, Premium'), 'Pro');
        await user.click(screen.getByRole('button', { name: 'Save' }));

        expect(mockCreateSubscription).toHaveBeenCalled();
        expect(mockNavigate).toHaveBeenCalledWith('/subscriptions');
    });

    it('shows a toast when saving fails', async () => {
        const user = userEvent.setup();
        mockUseSearchParams.mockReturnValue([new URLSearchParams('')]);
        mockUseGetSubscriptionByIdQuery.mockReturnValue({ data: undefined, isLoading: false, isError: false });
        mockCreateSubscription.mockReturnValue({ unwrap: () => Promise.reject({ status: 500, data: {} }) });

        render(<SubscriptionPage />);
        await user.click(screen.getByRole('button', { name: 'Save' }));

        expect(mockToastError).toHaveBeenCalledTimes(1);
    });

    it('cancel in create mode navigates back to the list', async () => {
        const user = userEvent.setup();
        mockUseSearchParams.mockReturnValue([new URLSearchParams('')]);
        mockUseGetSubscriptionByIdQuery.mockReturnValue({ data: undefined, isLoading: false, isError: false });

        render(<SubscriptionPage />);
        await user.click(screen.getByRole('button', { name: 'Cancel' }));

        expect(mockNavigate).toHaveBeenCalledWith('/subscriptions');
    });

    it('cancel in edit mode restores the fetched value without navigating', async () => {
        const user = userEvent.setup();
        mockUseSearchParams.mockReturnValue([new URLSearchParams('id=1')]);
        mockUseGetSubscriptionByIdQuery.mockReturnValue({
            data: { isSuccess: true, value: { id: 1, type: 'Basic', price: 10, durationInDays: 30 } },
            isLoading: false, isError: false,
        });

        render(<SubscriptionPage />);
        await user.click(screen.getByText('Edit'));
        await user.click(screen.getByRole('button', { name: 'Cancel' }));

        expect(mockNavigate).not.toHaveBeenCalled();
        expect(screen.getByText('Subscription Details')).toBeInTheDocument();
    });

    it('updates an existing subscription on save', async () => {
        const user = userEvent.setup();
        mockUseSearchParams.mockReturnValue([new URLSearchParams('id=1')]);
        mockUseGetSubscriptionByIdQuery.mockReturnValue({
            data: { isSuccess: true, value: { id: 1, type: 'Basic', price: 10, durationInDays: 30 } },
            isLoading: false, isError: false,
        });
        mockUpdateSubscription.mockReturnValue({ unwrap: () => Promise.resolve() });

        render(<SubscriptionPage />);
        await user.click(screen.getByText('Edit'));
        await user.click(screen.getByRole('button', { name: 'Save' }));

        expect(mockUpdateSubscription).toHaveBeenCalled();
        expect(mockNavigate).toHaveBeenCalledWith('/subscriptions');
    });
});
