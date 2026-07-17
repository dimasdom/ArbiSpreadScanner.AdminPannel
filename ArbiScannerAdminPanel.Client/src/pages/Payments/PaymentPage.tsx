import { usePayment } from "../../hooks/usePayment";
import { getPaymentStatus } from "../../types/accountType";
import ErrorState from "../../components/ErrorState";


function PaymentPage() {
    const { paymentModel, isLoading, isError } = usePayment();

    if (isLoading) {
        return <div className="max-w-5xl mx-auto mt-6">Loading...</div>;
    }

    if (isError) {
        return (
            <div className="max-w-5xl mx-auto mt-6 flex justify-center">
                <ErrorState message="Failed to load this payment. Please try again later." />
            </div>
        );
    }

    return (
        <div className="max-w-5xl mx-auto mt-6 shadow-2xl rounded-4xl bg-white">
            <div className="shadow-inner pb-4 px-4 sm:px-6 rounded-4xl lg:px-8 w-full">
                <div className="px-3 pb-2">
                    <div className="flex justify-between items-center mb-6">
                        <h2 className="text-2xl font-semibold">Payment Details</h2>
                    </div>
                    <div className="space-y-3">
                        <p className="text-lg font-medium">
                            <span className="text-gray-600">User: </span>
                            {paymentModel?.userEmail ?? '—'}
                        </p>
                        <p className="text-lg font-medium">
                            <span className="text-gray-600">Amount: </span>
                            ${paymentModel?.amount?.toFixed(2) ?? '—'}
                        </p>
                        <p className="text-lg font-medium">
                            <span className="text-gray-600">Payment Date: </span>
                            {paymentModel?.paymentDate ? new Date(paymentModel.paymentDate).toLocaleString() : '—'}
                        </p>
                        <p className="text-lg font-medium">
                            <span className="text-gray-600">Status: </span>
                            {paymentModel ? getPaymentStatus(paymentModel.status) : '—'}
                        </p>
                        <p className="text-lg font-medium">
                            <span className="text-gray-600">Transaction ID: </span>
                            <span className="font-mono text-sm">{paymentModel?.transactionId ?? '—'}</span>
                        </p>
                        {paymentModel?.paymentUrl && (
                            <p className="text-lg font-medium">
                                <span className="text-gray-600">Payment URL: </span>
                                <a href={paymentModel.paymentUrl} target="_blank" rel="noreferrer" className="text-blue-600 underline text-sm break-all">{paymentModel.paymentUrl}</a>
                            </p>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}

export default PaymentPage;