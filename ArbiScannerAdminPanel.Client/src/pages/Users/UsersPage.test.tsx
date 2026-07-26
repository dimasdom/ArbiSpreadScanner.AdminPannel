import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

const mockUseUsers = vi.fn();
vi.mock('../../hooks/useUsers', () => ({
    useUsers: () => mockUseUsers(),
}));

import UsersPage from './UsersPage';

describe('UsersPage', () => {
    it('shows an error state', () => {
        mockUseUsers.mockReturnValue({ isError: true });
        render(<UsersPage />);

        expect(screen.getByText('Failed to load users. Please try again later.')).toBeInTheDocument();
    });

    it('renders without a delete button when nothing is selected', () => {
        mockUseUsers.mockReturnValue({
            rows: [], paginationModel: { page: 1, pageSize: 10 }, selectedRows: undefined, allSelected: false,
            isLoading: false, isError: false, hasSelection: false,
            setPaginationModel: vi.fn(), handleRowDoubleClick: vi.fn(), handleRowClick: vi.fn(),
            handleRowSelectionChange: vi.fn(), handleDeleteSelected: vi.fn(),
        });
        render(<UsersPage />);

        expect(screen.queryByText(/Delete Selected/)).not.toBeInTheDocument();
    });

    it('shows a delete button with the selection count and triggers deletion', async () => {
        const user = userEvent.setup();
        const handleDeleteSelected = vi.fn();
        mockUseUsers.mockReturnValue({
            rows: [], paginationModel: { page: 1, pageSize: 10 }, selectedRows: { ids: new Set(['u1', 'u2']), type: 'include' }, allSelected: false,
            isLoading: false, isError: false, hasSelection: true,
            setPaginationModel: vi.fn(), handleRowDoubleClick: vi.fn(), handleRowClick: vi.fn(),
            handleRowSelectionChange: vi.fn(), handleDeleteSelected,
        });
        render(<UsersPage />);

        await user.click(screen.getByText('Delete Selected (2)'));

        expect(handleDeleteSelected).toHaveBeenCalledTimes(1);
    });
});
