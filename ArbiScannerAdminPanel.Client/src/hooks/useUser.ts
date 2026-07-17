import { useSearchParams } from "react-router";
import { useGetUserByIdQuery } from "../store/services/users";

export function useUser() {
    const [searchParams] = useSearchParams();
    const id = searchParams.get("id") ?? "";

    const { data, isLoading, isError } = useGetUserByIdQuery(id, { skip: !id });
    const userModel = data?.isSuccess ? data.value : null;

    return { userModel, isLoading, isError };
}
