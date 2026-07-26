import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

const mockUsePayments = vi.fn();
vi.mock('../../hooks/usePayments', () => ({
    usePayments: () => mockUsePayments(),
}));

import PaymentsPage from './PaymentsPage';

describe('PaymentsPage', () => {
    it('shows an error state', () => {
        mockUsePayments.mockReturnValue({ isError: true, rows: [], paginationModel: { page: 1, pageSize: 10 }, hasSelection: false });
        render(<PaymentsPage />);

        expect(screen.getByText('Failed to load payments. Please try again later.')).toBeInTheDocument();
    });

    it('renders the grid without a delete button when nothing is selected', () => {
        mockUsePayments.mockReturnValue({
            rows: [], paginationModel: { page: 1, pageSize: 10 }, selectedRows: undefined, allSelected: false,
            isLoading: false, isError: false, hasSelection: false,
            setPaginationModel: vi.fn(), handleRowDoubleClick: vi.fn(), handleRowClick: vi.fn(),
            handleRowSelectionChange: vi.fn(), handleDeleteSelected: vi.fn(),
        });
        render(<PaymentsPage />);

        expect(screen.queryByText(/Delete Selected/)).not.toBeInTheDocument();
    });

    it('shows a delete button with the selection count and triggers deletion', async () => {
        const user = userEvent.setup();
        const handleDeleteSelected = vi.fn();
        mockUsePayments.mockReturnValue({
            rows: [], paginationModel: { page: 1, pageSize: 10 }, selectedRows: { ids: new Set([1]), type: 'include' }, allSelected: false,
            isLoading: false, isError: false, hasSelection: true,
            setPaginationModel: vi.fn(), handleRowDoubleClick: vi.fn(), handleRowClick: vi.fn(),
            handleRowSelectionChange: vi.fn(), handleDeleteSelected,
        });
        render(<PaymentsPage />);

        const button = screen.getByText('Delete Selected (1)');
        await user.click(button);

        expect(handleDeleteSelected).toHaveBeenCalledTimes(1);
    });

    it('shows "All" when every row is selected', () => {
        mockUsePayments.mockReturnValue({
            rows: [], paginationModel: { page: 1, pageSize: 10 }, selectedRows: undefined, allSelected: true,
            isLoading: false, isError: false, hasSelection: true,
            setPaginationModel: vi.fn(), handleRowDoubleClick: vi.fn(), handleRowClick: vi.fn(),
            handleRowSelectionChange: vi.fn(), handleDeleteSelected: vi.fn(),
        });
        render(<PaymentsPage />);

        expect(screen.getByText('Delete Selected (All)')).toBeInTheDocument();
    });
});
