import { combineReducers, configureStore } from '@reduxjs/toolkit';
import { persistReducer, persistStore } from 'redux-persist';
import storage from 'redux-persist/es/storage';
import accountReducer, { type AccountState } from './slices/accountSlice';
import { accountApi } from './services/account';
import { subscriptionsAPI } from './services/subscriptions';
import { usersAPI } from './services/users';
import { paymentsAPI } from './services/payments';
import { userSubscriptionsAPI } from './services/userSubscriptions';

export interface IRootStore {
    account: AccountState;
}

const persistConfig = {
    key: 'root',
    storage,
    whitelist: ['account'],
    blacklist: [
        accountApi.reducerPath,
        subscriptionsAPI.reducerPath,
        usersAPI.reducerPath,
        paymentsAPI.reducerPath,
        userSubscriptionsAPI.reducerPath,
    ],
};

const rootReducer = combineReducers({
    account: accountReducer,
    [accountApi.reducerPath]: accountApi.reducer,
    [subscriptionsAPI.reducerPath]: subscriptionsAPI.reducer,
    [usersAPI.reducerPath]: usersAPI.reducer,
    [paymentsAPI.reducerPath]: paymentsAPI.reducer,
    [userSubscriptionsAPI.reducerPath]: userSubscriptionsAPI.reducer,
});

const persistedReducer = persistReducer(persistConfig, rootReducer);

const store = configureStore({
    reducer: persistedReducer,
    middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware({
            serializableCheck: false,
        })
            .concat(accountApi.middleware)
            .concat(subscriptionsAPI.middleware)
            .concat(usersAPI.middleware)
            .concat(paymentsAPI.middleware)
            .concat(userSubscriptionsAPI.middleware),
});

export const persistor = persistStore(store);

export default store;
