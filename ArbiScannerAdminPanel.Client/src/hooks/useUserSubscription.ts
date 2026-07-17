import { useNavigate, useSearchParams } from "react-router";
import { useEffect, useState } from "react";
import { toast } from "react-hot-toast";
import type { FetchBaseQueryError } from "@reduxjs/toolkit/query";
import type { SerializedError } from "@reduxjs/toolkit";
import type { UserSubscriptionModel } from "../types/accountType";
import { useGetUserSubscriptionByIdQuery, useUpdateUserSubscriptionMutation } from "../store/services/userSubscriptions";
import { normalizeApiError } from "../utils/normalizeApiError";

export function useUserSubscription() {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const subscriptionId = searchParams.get("id");

    const [userSubscriptionModel, setUserSubscriptionModel] = useState<UserSubscriptionModel>({
        id: 0,
        userId: "",
        subscriptionId: 0,
        subscription: null,
        startDate: "",
        endDate: "",
    });
    const [isEditMode, setIsEditMode] = useState(false);

    const { data, isLoading, isError } = useGetUserSubscriptionByIdQuery(Number(subscriptionId));
    const [updateUserSubscription] = useUpdateUserSubscriptionMutation();

    useEffect(() => {
        if (data && data.isSuccess && data.value) {
            setUserSubscriptionModel(data.value);
            setIsEditMode(false);
        }
    }, [data]);

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setUserSubscriptionModel((prev) => ({
            ...prev,
            [name]: value,
        }));
    };

    const isEndDateValid = () => {
        const endDate = new Date(userSubscriptionModel.endDate);
        return endDate > new Date();
    };

    const handleSave = async () => {
        if (!isEndDateValid()) {
            return;
        }
        try {
            await updateUserSubscription(userSubscriptionModel).unwrap();
            setIsEditMode(false);
            navigate("/userSubscriptions");
        } catch (error) {
            toast.error(normalizeApiError(error as FetchBaseQueryError | SerializedError).message);
        }
    };

    const handleCancel = () => {
        setIsEditMode(false);
        if (data && data.value) {
            setUserSubscriptionModel(data.value);
        }
    };

    return {
        userSubscriptionModel,
        isEditMode,
        isLoading,
        isError,
        isEndDateValid,
        setIsEditMode,
        handleInputChange,
        handleSave,
        handleCancel,
    };
}
