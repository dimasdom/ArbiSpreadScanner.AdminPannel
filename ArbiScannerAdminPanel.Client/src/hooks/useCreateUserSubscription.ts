import { useNavigate } from "react-router";
import { useEffect, useState } from "react";
import type { ClientAccountTableRowDTO } from "../types/accountType";
import { useGetSubscriptionsQuery } from "../store/services/subscriptions";
import { useGetUsersQuery } from "../store/services/users";
import { useCreateUserSubscriptionMutation } from "../store/services/userSubscriptions";

export function useCreateUserSubscription() {
    const navigate = useNavigate();
    const [email, setEmail] = useState("");
    const [subscriptionId, setSubscriptionId] = useState<number | null>(null);
    const [selectedUser, setSelectedUser] = useState<ClientAccountTableRowDTO | null>(null);
    const [userSearchResults, setUserSearchResults] = useState<ClientAccountTableRowDTO[]>([]);
    const [showUserDropdown, setShowUserDropdown] = useState(false);
    const [isSearching, setIsSearching] = useState(false);

    const { data: subscriptionsData, isLoading: subscriptionsLoading } = useGetSubscriptionsQuery(1);
    const { data: usersData } = useGetUsersQuery(1);
    const [createUserSubscription, { isLoading: isCreating }] = useCreateUserSubscriptionMutation();

    useEffect(() => {
        const timer = setTimeout(() => {
            if (email.trim() && usersData?.isSuccess && usersData.value) {
                setIsSearching(true);
                const filtered = usersData.value.filter(user =>
                    user.userMail?.toLowerCase().includes(email.toLowerCase())
                );
                setUserSearchResults(filtered);
                setShowUserDropdown(true);
                setIsSearching(false);
            } else {
                setUserSearchResults([]);
                setShowUserDropdown(false);
            }
        }, 300);

        return () => clearTimeout(timer);
    }, [email, usersData]);

    const handleEmailChange = (value: string) => {
        setEmail(value);
        setSelectedUser(null);
    };

    const handleUserSelect = (user: ClientAccountTableRowDTO) => {
        setSelectedUser(user);
        setEmail(user.userMail || "");
        setShowUserDropdown(false);
    };

    const handleCreate = async () => {
        if (!selectedUser || !subscriptionId) {
            return;
        }
        try {
            await createUserSubscription({
                userEmail: selectedUser.userMail,
                subscriptionId: subscriptionId
            }).unwrap();
            navigate("/userSubscriptions");
        } catch (error) {
            console.error("Error creating user subscription:", error);
        }
    };

    const handleCancel = () => navigate("/userSubscriptions");

    const subscriptions = subscriptionsData?.isSuccess ? subscriptionsData.value : [];

    return {
        email,
        subscriptionId,
        selectedUser,
        userSearchResults,
        showUserDropdown,
        isSearching,
        subscriptions,
        subscriptionsLoading,
        isCreating,
        setSubscriptionId,
        handleEmailChange,
        handleUserSelect,
        handleCreate,
        handleCancel,
    };
}
