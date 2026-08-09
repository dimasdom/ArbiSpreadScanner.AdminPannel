import { useEffect } from 'react';
import './App.css';
import NavBar from './components/NavBar';
import { Route, Routes, useLocation } from 'react-router';
import { AnimatePresence, motion } from 'framer-motion';
import { Toaster } from 'react-hot-toast';
import { useAuth } from 'react-oidc-context';
import AuthCallbackPage from './pages/Auth/AuthCallbackPage';
import UsersPage from './pages/Users/UsersPage';
import PaymentsPage from './pages/Payments/PaymentsPage';
import SubscriptionsPage from './pages/Subscriptions/SubscriptionsPage';
import UserPage from './pages/Users/UserPage';
import PaymentPage from './pages/Payments/PaymentPage';
import SubscriptionPage from './pages/Subscriptions/SubscriptionPage';
import UserSubscriptionPage from './pages/UserSubscriptions/UserSubscriptionPage';
import UserSubscriptionsPage from './pages/UserSubscriptions/UserSubscriptionsPage';
import CreateUserSubscriptionPage from './pages/UserSubscriptions/CreateUserSubscriptionPage';



const LoadingSpinner = () => (
    <div className="flex items-center justify-center min-h-64 mt-6">
        <div className="w-10 h-10 border-4 border-gray-200 border-t-indigo-500 rounded-full animate-spin" />
    </div>
);

function App() {
    const auth = useAuth();
    const location = useLocation();

    useEffect(() => {
        if (auth.isLoading || location.pathname === '/auth/callback') return;
        if (!auth.isAuthenticated) {
            void auth.signinRedirect();
        }
    }, [auth.isLoading, auth.isAuthenticated, auth, location.pathname]);

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
            <NavBar isLoggedIn={auth.isAuthenticated} onLogin={() => { void auth.signinRedirect(); }} onLogout={() => { void auth.signoutRedirect(); }} />
            <AnimatePresence mode="wait">
                <Routes location={location} key={location.pathname}>
                    <Route path="auth/callback" element={<AuthCallbackPage />} />
                    {auth.isAuthenticated ? (
                        <>
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
                        </>
                    ) : (
                        <Route path="*" element={<LoadingSpinner />} />
                    )}
                </Routes>
            </AnimatePresence>

            <Toaster position="bottom-right" />
        </>
    )
}

export default App;