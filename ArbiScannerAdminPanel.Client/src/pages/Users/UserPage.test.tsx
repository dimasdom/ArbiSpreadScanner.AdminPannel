import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

const mockUseUser = vi.fn();
vi.mock('../../hooks/useUser', () => ({
    useUser: () => mockUseUser(),
}));

import UserPage from './UserPage';

describe('UserPage', () => {
    it('shows a loading state', () => {
        mockUseUser.mockReturnValue({ userModel: null, isLoading: true, isError: false });
        render(<UserPage />);

        expect(screen.getByText('Loading...')).toBeInTheDocument();
    });

    it('shows an error state', () => {
        mockUseUser.mockReturnValue({ userModel: null, isLoading: false, isError: true });
        render(<UserPage />);

        expect(screen.getByText('Failed to load this user. Please try again later.')).toBeInTheDocument();
    });

    it('shows "no subscription"/"no payments" placeholders when absent', () => {
        mockUseUser.mockReturnValue({
            userModel: { id: 'u1', userMail: 'a@test.com', userName: 'alice', subscription: null, payments: [] },
            isLoading: false,
            isError: false,
        });
        render(<UserPage />);

        expect(screen.getByText('No active subscription')).toBeInTheDocument();
        expect(screen.getByText('No payments')).toBeInTheDocument();
    });

    it('renders subscription details and a payments table', () => {
        mockUseUser.mockReturnValue({
            userModel: {
                id: 'u1',
                userMail: 'a@test.com',
                userName: 'alice',
                subscription: { id: 1, userId: 'u1', subscriptionId: 1, subscription: { id: 1, type: 'Basic', price: 10, durationInDays: 30 } },
                payments: [{ id: 1, userId: 'u1', amount: 10, paymentDate: '2024-01-01T00:00:00Z', status: 1, transactionId: 'TRK1' }],
            },
            isLoading: false,
            isError: false,
        });
        render(<UserPage />);

        expect(screen.getByText('Basic')).toBeInTheDocument();
        expect(screen.getByText('30 days')).toBeInTheDocument();
        expect(screen.getByText('TRK1')).toBeInTheDocument();
    });
});
