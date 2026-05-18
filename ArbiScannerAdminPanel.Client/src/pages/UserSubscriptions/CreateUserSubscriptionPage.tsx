import type { SubscriptionModel } from "../../types/accountType";
import { useCreateUserSubscription } from "../../hooks/useCreateUserSubscription";

function CreateUserSubscriptionPage() {
    const {
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
    } = useCreateUserSubscription();

    return (
        <div className="max-w-5xl mx-auto mt-6 shadow-2xl rounded-4xl bg-white">
            <div className="shadow-inner pb-4 px-4 sm:px-6 rounded-4xl lg:px-8 w-full">
                <div className="px-3 pb-2 pt-4">
                    <h2 className="text-2xl font-semibold mb-6">Create User Subscription</h2>

                    <div className="space-y-6">
                        {/* Email Search */}
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-2">Email</label>
                            <div className="relative">
                                <input
                                    type="email"
                                    value={email}
                                    onChange={(e) => handleEmailChange(e.target.value)}
                                    placeholder="Search user by email..."
                                    className="w-full px-3 py-2 border bg-white rounded-lg focus:ring-2 focus:ring-blue-600 outline-none"
                                />
                                {isSearching && (
                                    <div className="absolute right-3 top-2.5">
                                        <div className="animate-spin h-5 w-5 border-2 border-blue-600 border-t-transparent rounded-full"></div>
                                    </div>
                                )}
                                {showUserDropdown && userSearchResults.length > 0 && (
                                    <div className="absolute z-10 w-full mt-1 bg-white  rounded-lg shadow-lg max-h-48 overflow-y-auto">
                                        {userSearchResults.map((user) => (
                                            <button
                                                key={user.id}
                                                onClick={() => handleUserSelect(user)}
                                                className="w-full text-left px-4 py-2 bg-white hover:bg-blue-50 border-b last:border-b-0"
                                            >
                                                <p className="font-medium">{user.userMail}</p>
                                            </button>
                                        ))}
                                    </div>
                                )}
                            </div>
                            {selectedUser && (
                                <p className="text-sm text-green-600 mt-2">✓ User selected: {selectedUser.userMail}</p>
                            )}
                        </div>

                        {/* Subscription Select */}
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-2">Subscription</label>
                            <select
                                value={subscriptionId || ""}
                                onChange={(e) => setSubscriptionId(Number(e.target.value))}
                                className="w-full px-3 py-2 border bg-white rounded-lg focus:ring-2 focus:ring-blue-600 outline-none"
                            >
                                <option value="">Select a subscription...</option>
                                {subscriptions.map((sub: SubscriptionModel) => (
                                    <option key={sub.id} value={sub.id}>
                                        {sub.type} - ${sub.price.toFixed(2)}
                                    </option>
                                ))}
                            </select>
                        </div>

                        {/* Action Buttons */}
                        <div className="flex gap-3 mt-8">
                            <button
                                onClick={handleCreate}
                                disabled={!selectedUser || !subscriptionId || isCreating || subscriptionsLoading}
                                className="px-6 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:bg-gray-400 disabled:cursor-not-allowed font-medium"
                            >
                                {isCreating ? "Creating..." : "Create"}
                            </button>
                            <button
                                onClick={handleCancel}
                                className="px-6 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500 font-medium"
                            >
                                Cancel
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default CreateUserSubscriptionPage;