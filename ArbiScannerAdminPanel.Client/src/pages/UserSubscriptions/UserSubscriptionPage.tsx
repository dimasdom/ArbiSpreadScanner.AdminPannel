import { useUserSubscription } from "../../hooks/useUserSubscription";
import ErrorState from "../../components/ErrorState";

function UserSubscriptionPage() {
    const {
        userSubscriptionModel,
        isEditMode,
        isLoading,
        isError,
        isEndDateValid,
        setIsEditMode,
        handleInputChange,
        handleSave,
        handleCancel,
    } = useUserSubscription();

    if (isLoading) {
        return <div className="max-w-5xl mx-auto mt-6">Loading...</div>;
    }

    if (isError) {
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
                            {"Subscription Details"}
                        </h2>
                        {!isEditMode && (
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
                                <label className="block text-sm font-medium text-gray-700">End Date</label>
                                <input
                                    type="date"
                                    name="endDate"
                                    value={userSubscriptionModel.endDate ? new Date(userSubscriptionModel.endDate).toISOString().split('T')[0] : ''}
                                    onChange={handleInputChange}
                                    min={new Date().toISOString().slice(0, 10)}
                                    className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-600 outline-none"
                                />
                                {userSubscriptionModel.endDate && !isEndDateValid() && (
                                    <p className="text-red-600 text-sm mt-1">End date cannot be in the past</p>
                                )}
                            </div>

                            <div className="flex gap-3 mt-6">
                                <button
                                    onClick={handleSave}
                                    disabled={!isEndDateValid()}
                                    className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
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
                                {userSubscriptionModel.subscription?.type}
                            </p>
                            <p className="text-lg font-medium">
                                <span className="text-gray-600">Price: </span>${userSubscriptionModel.subscription?.price.toFixed(2)}
                            </p>
                            <p className="text-lg font-medium">
                                <span className="text-gray-600">Duration: </span>
                                {userSubscriptionModel.subscription?.durationInDays} days
                            </p>
                            <p className="text-lg font-medium">
                                <span className="text-gray-600">Start date: </span>
                                {userSubscriptionModel.startDate ? new Date(userSubscriptionModel.startDate).toLocaleString() : '—'}
                            </p>
                            <p className="text-lg font-medium">
                                <span className="text-gray-600">End date: </span>
                                {userSubscriptionModel.endDate ? new Date(userSubscriptionModel.endDate).toLocaleString() : '—'}
                            </p>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}

export default UserSubscriptionPage;