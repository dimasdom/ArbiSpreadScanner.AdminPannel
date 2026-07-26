import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

const mockUsePayment = vi.fn();
vi.mock('../../hooks/usePayment', () => ({
    usePayment: () => mockUsePayment(),
}));

import PaymentPage from './PaymentPage';

describe('PaymentPage', () => {
    it('shows a loading state', () => {
        mockUsePayment.mockReturnValue({ paymentModel: null, isLoading: true, isError: false });
        render(<PaymentPage />);

        expect(screen.getByText('Loading...')).toBeInTheDocument();
    });

    it('shows an error state', () => {
        mockUsePayment.mockReturnValue({ paymentModel: null, isLoading: false, isError: true });
        render(<PaymentPage />);

        expect(screen.getByText('Failed to load this payment. Please try again later.')).toBeInTheDocument();
    });

    it('renders payment details including the optional payment URL', () => {
        mockUsePayment.mockReturnValue({
            paymentModel: {
                id: 1,
                userId: 'u1',
                userEmail: 'a@test.com',
                amount: 12.5,
                paymentUrl: 'https://pay.example.com/1',
                paymentDate: '2024-01-01T00:00:00Z',
                status: 1,
                transactionId: 'TRK1',
            },
            isLoading: false,
            isError: false,
        });
        render(<PaymentPage />);

        expect(screen.getByText('a@test.com')).toBeInTheDocument();
        expect(screen.getByText('$12.50')).toBeInTheDocument();
        expect(screen.getByText('Completed')).toBeInTheDocument();
        expect(screen.getByText('TRK1')).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'https://pay.example.com/1' })).toBeInTheDocument();
    });

    it('renders placeholders when fields are missing', () => {
        mockUsePayment.mockReturnValue({
            paymentModel: null,
            isLoading: false,
            isError: false,
        });
        render(<PaymentPage />);

        expect(screen.getAllByText('—').length).toBeGreaterThan(0);
    });
});
