import { useEffect } from 'react';
import './App.css';
import NavBar from './components/NavBar';
import { Route, Routes, useNavigate, useLocation } from 'react-router';
import { AnimatePresence, motion } from 'framer-motion';
import LoginPage from './pages/Account/LoginPage';
import { useSelector } from 'react-redux';
import type { IRootStore } from './store/store';
import { useLogoutMutation } from './store/services/account';
import UsersPage from './pages/Users/UsersPage';
import PaymentsPage from './pages/Payments/PaymentsPage';
import SubscriptionsPage from './pages/Subscriptions/SubscriptionsPage';
import UserPage from './pages/Users/UserPage';
import PaymentPage from './pages/Payments/PaymentPage';
import SubscriptionPage from './pages/Subscriptions/SubscriptionPage';
import UserSubscriptionPage from './pages/UserSubscriptions/UserSubscriptionPage';
import UserSubscriptionsPage from './pages/UserSubscriptions/UserSubscriptionsPage';
import CreateUserSubscriptionPage from './pages/UserSubscriptions/CreateUserSubscriptionPage';



function App() {
    const isLoggedIn = useSelector((state: IRootStore) => state.account.isLoggedIn);
    const navigator = useNavigate();
    const [logoutMutation] = useLogoutMutation();
    useEffect(() => {
        if (!isLoggedIn) {
            navigator("/account/login");
        };
    }, [isLoggedIn, navigator]);
    const location = useLocation();

    const PageWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
        <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.18 }}
            className="min-h-full"
        >
            {children}
        </motion.div>
    );

    return (
        <>
            <NavBar isLoggedIn={isLoggedIn} onLogin={() => { navigator("/account/login") }} onLogout={() => { logoutMutation(); }} />
            {isLoggedIn ?
                <AnimatePresence mode="wait">
                    <Routes location={location} key={location.pathname}>
                        <Route path="account">
                            <Route path="login" element={<PageWrapper><LoginPage /></PageWrapper>} />
                        </Route>
                        <Route index element={<PageWrapper><UsersPage /></PageWrapper>} />
                        <Route path="users" element={<PageWrapper><UsersPage /></PageWrapper>} />
                        <Route path="user" element={<PageWrapper><UserPage /></PageWrapper>} />
                        <Route path="payments" element={<PageWrapper><PaymentsPage /></PageWrapper>} />
                        <Route path="payment" element={<PageWrapper><PaymentPage /></PageWrapper>} />
                        <Route path="Subscriptions" element={<PageWrapper><SubscriptionsPage /></PageWrapper>} />
                        <Route path="Subscription" element={<PageWrapper><SubscriptionPage /></PageWrapper>} />
                        <Route path="UserSubscriptions" element={<PageWrapper><UserSubscriptionsPage /></PageWrapper>} />
                        <Route path="UserSubscription" element={<PageWrapper><UserSubscriptionPage /></PageWrapper>} />
                        <Route path="CreateUserSubscription" element={<PageWrapper><CreateUserSubscriptionPage /></PageWrapper>} />
                    </Routes>
                </AnimatePresence> :
                <AnimatePresence mode="wait">
                    <Routes location={location} key={location.pathname}>
                        <Route path="account">
                            <Route path="login" element={<PageWrapper><LoginPage /></PageWrapper>} />
                        </Route>
                    </Routes>
                </AnimatePresence>}


        </>
    )
}

export default App;