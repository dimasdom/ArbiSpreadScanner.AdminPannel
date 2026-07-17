import { useSearchParams, useNavigate } from "react-router";
import { useEffect, useState } from "react";
import { toast } from "react-hot-toast";
import type { FetchBaseQueryError } from "@reduxjs/toolkit/query";
import type { SerializedError } from "@reduxjs/toolkit";
import type { SubscriptionModel } from "../../types/accountType";
import { useGetSubscriptionByIdQuery, useCreateSubscriptionMutation, useUpdateSubscriptionMutation } from "../../store/services/subscriptions";
import { normalizeApiError } from "../../utils/normalizeApiError";
import ErrorState from "../../components/ErrorState";

function SubscriptionPage() {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const subscriptionId = searchParams.get("id");
    const isCreateMode = !subscriptionId;

    const [subscriptionModel, setSubscriptionModel] = useState<SubscriptionModel>({
        id: 0,
        type: "",
        price: 0,
        durationInDays: 0,
    });
    const [isEditMode, setIsEditMode] = useState(isCreateMode);

    const { data, isLoading, isError } = useGetSubscriptionByIdQuery(Number(subscriptionId), {
        skip: isCreateMode,
    });
    const [createSubscription] = useCreateSubscriptionMutation();
    const [updateSubscription] = useUpdateSubscriptionMutation();

    useEffect(() => {
        if (data && data.isSuccess && data.value) {
            setSubscriptionModel(data.value);
            setIsEditMode(false);
        }
    }, [data]);

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setSubscriptionModel((prev) => ({
            ...prev,
            [name]: name === "price" || name === "durationInDays" ? parseFloat(value) : value,
        }));
    };

    const handleSave = async () => {
        try {
            if (isCreateMode) {
                await createSubscription(subscriptionModel).unwrap();
            } else {
                await updateSubscription(subscriptionModel).unwrap();
            }
            setIsEditMode(false);
            navigate("/subscriptions");
        } catch (error) {
            toast.error(normalizeApiError(error as FetchBaseQueryError | SerializedError).message);
        }
    };

    const handleCancel = () => {
        if (isCreateMode) {
            navigate("/subscriptions");
        } else {
            setIsEditMode(false);
            if (data && data.value) {
                setSubscriptionModel(data.value);
            }
        }
    };

    if (isLoading && !isCreateMode) {
        return <div className="max-w-5xl mx-auto mt-6">Loading...</div>;
    }

    if (isError && !isCreateMode) {
        return (
            <div className="max-w-5xl mx-auto mt-6 flex justify-center">
                <ErrorState message="Failed to load this subscription. Please try again later." />
            </div>
        );
    }

    return (
        <div className="max-w-5xl mx-auto mt-6 shadow-2xl rounded-4xl bg-white">
            <div className="shadow-inner pb-4 px-4 sm:px-6 rounded-4xl lg:px-8 w-full">
                <div className="px-3 pb-2">
                    <div className="flex justify-between items-center mb-6">
                        <h2 className="text-2xl font-semibold">
                            {isCreateMode ? "Create Subscription" : "Subscription Details"}
                        </h2>
                        {!isCreateMode && !isEditMode && (
                            <button
                                onClick={() => setIsEditMode(true)}
                                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
                            >
                                Edit
                            </button>
                        )}
                    </div>

                    {isEditMode ? (
                        <div className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-700">Type</label>
                                <input
                                    type="text"
                                    name="type"
                                    value={subscriptionModel.type}
                                    onChange={handleInputChange}
                                    className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-600 outline-none"
                                    placeholder="e.g., Basic, Standard, Premium"
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700">Price</label>
                                <input
                                    type="number"
                                    name="price"
                                    value={subscriptionModel.price}
                                    onChange={handleInputChange}
                                    step="0.01"
                                    className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-600 outline-none"
                                    placeholder="0.00"
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700">Duration (Days)</label>
                                <input
                                    type="number"
                                    name="durationInDays"
                                    value={subscriptionModel.durationInDays}
                                    onChange={handleInputChange}
                                    className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-600 outline-none"
                                    placeholder="30"
                                />
                            </div>

                            <div className="flex gap-3 mt-6">
                                <button
                                    onClick={handleSave}
                                    className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700"
                                >
                                    Save
                                </button>
                                <button
                                    onClick={handleCancel}
                                    className="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500"
                                >
                                    Cancel
                                </button>
                            </div>
                        </div>
                    ) : (
                        <div className="space-y-3">
                            <p className="text-lg font-medium">
                                <span className="text-gray-600">Type: </span>
                                {subscriptionModel.type}
                            </p>
                            <p className="text-lg font-medium">
                                <span className="text-gray-600">Price: </span>${subscriptionModel.price.toFixed(2)}
                            </p>
                            <p className="text-lg font-medium">
                                <span className="text-gray-600">Duration: </span>
                                {subscriptionModel.durationInDays} days
                            </p>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}

export default SubscriptionPage;