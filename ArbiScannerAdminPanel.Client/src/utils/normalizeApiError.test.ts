import { describe, expect, it } from 'vitest';
import { normalizeApiError } from './normalizeApiError';
import type { FetchBaseQueryError } from '@reduxjs/toolkit/query';
import type { SerializedError } from '@reduxjs/toolkit';

describe('normalizeApiError', () => {
    it('returns UNKNOWN for undefined error', () => {
        const result = normalizeApiError(undefined);

        expect(result).toEqual({ code: 'UNKNOWN', message: 'Something went wrong. Please try again.' });
    });

    it('extracts code and message from an ApiErrorBody payload', () => {
        const error: FetchBaseQueryError = {
            status: 400,
            data: { isSuccess: false, errorCode: 'VALIDATION_FAILED', message: 'Email is required' },
        };

        const result = normalizeApiError(error);

        expect(result).toEqual({ code: 'VALIDATION_FAILED', message: 'Email is required' });
    });

    it('maps FETCH_ERROR to a network error', () => {
        const error: FetchBaseQueryError = { status: 'FETCH_ERROR', error: 'failed to fetch' };

        const result = normalizeApiError(error);

        expect(result.code).toBe('NETWORK_ERROR');
    });

    it('maps TIMEOUT_ERROR to a network error', () => {
        const error: FetchBaseQueryError = { status: 'TIMEOUT_ERROR', error: 'timeout' };

        const result = normalizeApiError(error);

        expect(result.code).toBe('NETWORK_ERROR');
    });

    it('maps PARSING_ERROR to a network error', () => {
        const error: FetchBaseQueryError = { status: 'PARSING_ERROR', originalStatus: 200, data: '', error: 'bad json' };

        const result = normalizeApiError(error);

        expect(result.code).toBe('NETWORK_ERROR');
    });

    it('falls back to UNKNOWN when the fetch error body is not an ApiErrorBody', () => {
        const error: FetchBaseQueryError = { status: 500, data: 'raw text' };

        const result = normalizeApiError(error);

        expect(result).toEqual({ code: 'UNKNOWN', message: 'Something went wrong. Please try again.' });
    });

    it('uses the message from a SerializedError', () => {
        const error: SerializedError = { name: 'Error', message: 'boom' };

        const result = normalizeApiError(error);

        expect(result).toEqual({ code: 'CLIENT_ERROR', message: 'boom' });
    });

    it('falls back to a default message when SerializedError has no message', () => {
        const error: SerializedError = { name: 'Error' };

        const result = normalizeApiError(error);

        expect(result).toEqual({ code: 'CLIENT_ERROR', message: 'Something went wrong. Please try again.' });
    });
});
