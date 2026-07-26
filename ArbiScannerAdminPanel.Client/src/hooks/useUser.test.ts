import { describe, expect, it, vi } from 'vitest';
import { renderHook } from '@testing-library/react';

const mockUseSearchParams = vi.fn();
vi.mock('react-router', () => ({
    useSearchParams: () => mockUseSearchParams(),
}));

const mockUseGetUserByIdQuery = vi.fn();
vi.mock('../store/services/users', () => ({
    useGetUserByIdQuery: (...args: unknown[]) => mockUseGetUserByIdQuery(...args),
}));

import { useUser } from './useUser';

describe('useUser', () => {
    it('skips the query and returns null when no id is present', () => {
        mockUseSearchParams.mockReturnValue([new URLSearchParams('')]);
        mockUseGetUserByIdQuery.mockReturnValue({ data: undefined, isLoading: false, isError: false });

        const { result } = renderHook(() => useUser());

        expect(mockUseGetUserByIdQuery).toHaveBeenCalledWith('', { skip: true });
        expect(result.current.userModel).toBeNull();
    });

    it('returns the user model when the query succeeds', () => {
        mockUseSearchParams.mockReturnValue([new URLSearchParams('id=u1')]);
        const user = { id: 'u1', userMail: 'a@test.com' };
        mockUseGetUserByIdQuery.mockReturnValue({ data: { isSuccess: true, value: user }, isLoading: false, isError: false });

        const { result } = renderHook(() => useUser());

        expect(mockUseGetUserByIdQuery).toHaveBeenCalledWith('u1', { skip: false });
        expect(result.current.userModel).toBe(user);
    });
});
