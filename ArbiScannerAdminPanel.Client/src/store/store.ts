import { combineReducers, configureStore } from '@reduxjs/toolkit';
import { subscriptionsAPI } from './services/subscriptions';
import { usersAPI } from './services/users';
import { paymentsAPI } from './services/payments';
import { userSubscriptionsAPI } from './services/userSubscriptions';

export interface IRootStore {
    [subscriptionsAPI.reducerPath]: ReturnType<typeof subscriptionsAPI.reducer>;
    [usersAPI.reducerPath]: ReturnType<typeof usersAPI.reducer>;
    [paymentsAPI.reducerPath]: ReturnType<typeof paymentsAPI.reducer>;
    [userSubscriptionsAPI.reducerPath]: ReturnType<typeof userSubscriptionsAPI.reducer>;
}

const rootReducer = combineReducers({
    [subscriptionsAPI.reducerPath]: subscriptionsAPI.reducer,
    [usersAPI.reducerPath]: usersAPI.reducer,
    [paymentsAPI.reducerPath]: paymentsAPI.reducer,
    [userSubscriptionsAPI.reducerPath]: userSubscriptionsAPI.reducer,
});

const store = configureStore({
    reducer: rootReducer,
    middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware({
            serializableCheck: false,
        })
            .concat(subscriptionsAPI.middleware)
            .concat(usersAPI.middleware)
            .concat(paymentsAPI.middleware)
            .concat(userSubscriptionsAPI.middleware),
});

export default store;
