import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

export interface AccountState {
    isLoggedIn: boolean;
    loading: boolean;
    error: string | null;
    sessionChecked: boolean;
}

const initialState: AccountState = {
    isLoggedIn: false,
    loading: false,
    error: null,
    sessionChecked: false,
};

const accountSlice = createSlice({
    name: 'account',
    initialState,
    reducers: {
        setAuthenticated(state) {
            state.isLoggedIn = true;
            state.loading = false;
            state.error = null;
            state.sessionChecked = true;
        },
        setLoading(state, action: PayloadAction<boolean>) {
            state.loading = action.payload;
        },
        setError(state, action: PayloadAction<string | null>) {
            state.error = action.payload;
        },
        clearError(state) {
            state.error = null;
        },
        markSessionChecked(state) {
            state.sessionChecked = true;
        },
        logout(state) {
            state.isLoggedIn = false;
            state.loading = false;
            state.error = null;
            state.sessionChecked = true;
        },
    },
});

export const {
    setAuthenticated,
    setLoading,
    setError,
    clearError,
    markSessionChecked,
    logout,
} = accountSlice.actions;
export default accountSlice.reducer;