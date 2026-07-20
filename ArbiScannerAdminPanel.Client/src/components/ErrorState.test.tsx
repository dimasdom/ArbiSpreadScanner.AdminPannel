import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ErrorState from './ErrorState';

describe('ErrorState', () => {
    it('renders default title and message when no props are given', () => {
        render(<ErrorState />);

        expect(screen.getByText('Error')).toBeInTheDocument();
        expect(screen.getByText('Something went wrong. Please try again later.')).toBeInTheDocument();
    });

    it('renders custom title and message', () => {
        render(<ErrorState title="Load failed" message="Could not load users" />);

        expect(screen.getByText('Load failed')).toBeInTheDocument();
        expect(screen.getByText('Could not load users')).toBeInTheDocument();
    });

    it('does not render a retry button when onRetry is not provided', () => {
        render(<ErrorState />);

        expect(screen.queryByRole('button', { name: 'Try again' })).not.toBeInTheDocument();
    });

    it('renders a retry button and invokes onRetry when clicked', async () => {
        const user = userEvent.setup();
        const onRetry = vi.fn();
        render(<ErrorState onRetry={onRetry} />);

        await user.click(screen.getByRole('button', { name: 'Try again' }));

        expect(onRetry).toHaveBeenCalledTimes(1);
    });
});
