import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

const mockUseUserSubscriptions = vi.fn();
vi.mock('../../hooks/useUserSubscriptions', () => ({
    useUserSubscriptions: () => mockUseUserSubscriptions(),
}));

import UserSubscriptionsPage from './UserSubscriptionsPage';

describe('UserSubscriptionsPage', () => {
    it('shows an error state', () => {
        mockUseUserSubscriptions.mockReturnValue({ isError: true });
        render(<UserSubscriptionsPage />);

        expect(screen.getByText('Failed to load user subscriptions. Please try again later.')).toBeInTheDocument();
    });

    it('invokes handleCreate from the create button', async () => {
        const user = userEvent.setup();
        const handleCreate = vi.fn();
        mockUseUserSubscriptions.mockReturnValue({
            rows: [], paginationModel: { page: 1, pageSize: 10 }, selectedRows: undefined, allSelected: false,
            isLoading: false, isError: false, hasSelection: false,
            setPaginationModel: vi.fn(), handleRowDoubleClick: vi.fn(), handleRowClick: vi.fn(),
            handleRowSelectionChange: vi.fn(), handleDeleteSelected: vi.fn(), handleCreate,
        });
        render(<UserSubscriptionsPage />);

        await user.click(screen.getByText('Create'));

        expect(handleCreate).toHaveBeenCalledTimes(1);
    });

    it('shows a delete button with the selection count and triggers deletion', async () => {
        const user = userEvent.setup();
        const handleDeleteSelected = vi.fn();
        mockUseUserSubscriptions.mockReturnValue({
            rows: [], paginationModel: { page: 1, pageSize: 10 }, selectedRows: { ids: new Set([1, 2]), type: 'include' }, allSelected: false,
            isLoading: false, isError: false, hasSelection: true,
            setPaginationModel: vi.fn(), handleRowDoubleClick: vi.fn(), handleRowClick: vi.fn(),
            handleRowSelectionChange: vi.fn(), handleDeleteSelected, handleCreate: vi.fn(),
        });
        render(<UserSubscriptionsPage />);

        await user.click(screen.getByText('Delete Selected (2)'));

        expect(handleDeleteSelected).toHaveBeenCalledTimes(1);
    });

    it('shows "All" when every row is selected', () => {
        mockUseUserSubscriptions.mockReturnValue({
            rows: [], paginationModel: { page: 1, pageSize: 10 }, selectedRows: undefined, allSelected: true,
            isLoading: false, isError: false, hasSelection: true,
            setPaginationModel: vi.fn(), handleRowDoubleClick: vi.fn(), handleRowClick: vi.fn(),
            handleRowSelectionChange: vi.fn(), handleDeleteSelected: vi.fn(), handleCreate: vi.fn(),
        });
        render(<UserSubscriptionsPage />);

        expect(screen.getByText('Delete Selected (All)')).toBeInTheDocument();
    });
});
