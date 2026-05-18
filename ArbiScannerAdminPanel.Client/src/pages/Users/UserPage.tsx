import { getPaymentStatus } from "../../types/accountType";
import { useUser } from "../../hooks/useUser";

function UserPage() {
    const { userModel, isLoading } = useUser();

    if (isLoading) return <div className="max-w-5xl mx-auto mt-6 p-6 text-gray-500">Loading...</div>;

    return (
        <div className="max-w-5xl mx-auto mt-6 shadow-2xl rounded-4xl bg-white">
            <div className="shadow-inner pb-4 px-4 sm:px-6 rounded-4xl lg:px-8 w-full">
                <div className="px-3 pb-2">
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4 items-start">
                        <div className="md:col-span-2 space-y-2 pt-4">
                            <h2 className="text-2xl font-semibold">{userModel?.userMail ?? '—'}</h2>
                            <p className="text-sm text-gray-600">Username: <span className="font-medium">{userModel?.userName ?? '—'}</span></p>

                            <div className="mt-4">
                                <h3 className="text-base font-semibold text-gray-700">Subscription</h3>
                                {userModel?.subscription ? (
                                    <div className="text-sm text-gray-600 space-y-1 mt-1">
                                        <p>Type: <span className="font-medium">{userModel.subscription.subscription?.type ?? '—'}</span></p>
                                        <p>Price: <span className="font-medium">{userModel.subscription.subscription?.price ?? '—'}</span></p>
                                        <p>Duration: <span className="font-medium">{userModel.subscription.subscription?.durationInDays} days</span></p>
                                    </div>
                                ) : (
                                    <p className="text-sm text-gray-400 mt-1">No active subscription</p>
                                )}
                            </div>

                            <div className="mt-4">
                                <h3 className="text-base font-semibold text-gray-700">Payments</h3>
                                {userModel?.payments?.length ? (
                                    <table className="mt-2 w-full text-sm text-left text-gray-600 border-collapse">
                                        <thead>
                                            <tr className="border-b text-gray-500">
                                                <th className="py-1 pr-4">Amount</th>
                                                <th className="py-1 pr-4">Date</th>
                                                <th className="py-1 pr-4">Status</th>
                                                <th className="py-1">Transaction ID</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {userModel.payments.map((p) => (
                                                <tr key={p.id} className="border-b last:border-0">
                                                    <td className="py-1 pr-4">{p.amount}</td>
                                                    <td className="py-1 pr-4">{new Date(p.paymentDate).toLocaleDateString()}</td>
                                                    <td className="py-1 pr-4">{getPaymentStatus(p.status)}</td>
                                                    <td className="py-1 font-mono text-xs">{p.transactionId}</td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                ) : (
                                    <p className="text-sm text-gray-400 mt-1">No payments</p>
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default UserPage;