import { useSearchParams } from "react-router";
import { useGetPaymentByIdQuery } from "../store/services/payments";

export function usePayment() {
    const [searchParams] = useSearchParams();
    const paymentId = searchParams.get("id");

    const { data, isLoading, isError } = useGetPaymentByIdQuery(Number(paymentId), { skip: !paymentId });
    const paymentModel = data?.isSuccess ? data.value : null;

    return { paymentModel, isLoading, isError };
}
