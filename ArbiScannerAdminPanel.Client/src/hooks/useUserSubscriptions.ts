import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import type { GridRowParams, GridRowSelectionModel } from "@mui/x-data-grid";
import type { UserSubscriptionRowDTO } from "../types/accountType";
import { useDeleteUserSubscriptionsMutation, useGetUserSubscriptionsQuery } from "../store/services/userSubscriptions";

export function useUserSubscriptions() {
    const navigate = useNavigate();
    const [rows, setRows] = useState<Record<string, unknown>[]>([]);
    const [isMobile, setIsMobile] = useState(window.innerWidth < 768);
    const [paginationModel, setPaginationModel] = useState({ pageSize: 10, page: 1 });
    const [selectedRows, setSelectedRows] = useState<GridRowSelectionModel | undefined>(undefined);
    const [allSelected, setAllSelected] = useState<boolean>(false);

    const { data, isLoading } = useGetUserSubscriptionsQuery(paginationModel.page);
    const [deleteUserSubscriptions] = useDeleteUserSubscriptionsMutation();

    useEffect(() => {
        const handleResize = () => setIsMobile(window.innerWidth < 768);
        window.addEventListener("resize", handleResize);
        return () => window.removeEventListener("resize", handleResize);
    }, []);

    useEffect(() => {
        const result = data ?? { isSuccess: false, isFailed: true, errors: [], reasons: [], value: [] };
        if (result.isSuccess && result.value) {
            const mappedRows = result.value.map((item: UserSubscriptionRowDTO) => ({
                id: item.id,
                userMail: item.userMail,
                subcriptionType: item.subcriptionType,
                subscriptionStartDate: item.subscriptionStartDate,
                subscriptionEndDate: item.subscriptionEndDate,
            }));
            setRows(mappedRows);
        }
    }, [data]);

    const handleRowDoubleClick = (params: GridRowParams) => {
        navigate(`/usersubscription?id=${params.row.id}`);
    };

    const handleRowClick = (params: GridRowParams) => {
        if (isMobile) {
            navigate(`/usersubscription?id=${params.row.id}`);
        }
    };

    const handleRowSelectionChange = (newSelection: GridRowSelectionModel) => {
        setAllSelected(newSelection.type === "exclude");
        setSelectedRows(newSelection);
    };

    const handleDeleteSelected = async () => {
        if (!selectedRows && !allSelected) return;

        try {
            if (allSelected) {
                const idsToDelete = rows.map((r: Record<string, unknown>) => Number(r.id));
                deleteUserSubscriptions(idsToDelete);
            } else if (selectedRows) {
                const idsToDelete = Array.from(selectedRows.ids).map(id => Number(id.toString()));
                deleteUserSubscriptions(idsToDelete);
            }
            setAllSelected(false);
            setSelectedRows(undefined);
        } catch (error) {
            console.error("Error deleting subscriptions:", error);
        }
    };

    const handleCreate = () => navigate("/createusersubscription");

    const hasSelection = !!(selectedRows?.ids.size || allSelected);

    return {
        rows,
        isMobile,
        paginationModel,
        selectedRows,
        allSelected,
        isLoading,
        hasSelection,
        setPaginationModel,
        handleRowDoubleClick,
        handleRowClick,
        handleRowSelectionChange,
        handleDeleteSelected,
        handleCreate,
    };
}
