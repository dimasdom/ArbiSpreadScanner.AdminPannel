import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import SuccessComponent from './SuccessComponent';

describe('SuccessComponent', () => {
    it('renders the default title with the given message', () => {
        render(<SuccessComponent message="Saved successfully" />);

        expect(screen.getByText('Success')).toBeInTheDocument();
        expect(screen.getByText('Saved successfully')).toBeInTheDocument();
    });

    it('renders a custom title when provided', () => {
        render(<SuccessComponent title="Great job" message="All done" />);

        expect(screen.getByText('Great job')).toBeInTheDocument();
    });
});
