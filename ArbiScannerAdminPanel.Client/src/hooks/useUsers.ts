import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import type { GridRowParams, GridRowSelectionModel } from "@mui/x-data-grid";
import type { ClientAccountTableRowDTO } from "../types/accountType";
import { useDeleteUsersMutation, useGetUsersQuery } from "../store/services/users";

export function useUsers() {
    const navigate = useNavigate();
    const [rows, setRows] = useState<Record<string, unknown>[]>([]);
    const [isMobile, setIsMobile] = useState(window.innerWidth < 768);
    const [paginationModel, setPaginationModel] = useState({ pageSize: 10, page: 1 });
    const [selectedRows, setSelectedRows] = useState<GridRowSelectionModel | undefined>(undefined);
    const [allSelected, setAllSelected] = useState<boolean>(false);

    const { data, isLoading } = useGetUsersQuery(paginationModel.page);
    const [deleteUsers] = useDeleteUsersMutation();

    useEffect(() => {
        const handleResize = () => setIsMobile(window.innerWidth < 768);
        window.addEventListener("resize", handleResize);
        return () => window.removeEventListener("resize", handleResize);
    }, []);

    useEffect(() => {
        const result = data ?? { isSuccess: false, isFailed: true, errors: [], reasons: [], value: [] };
        if (result.isSuccess && result.value) {
            const mappedRows = result.value.map((item: ClientAccountTableRowDTO) => ({
                id: item.id,
                userMail: item.userMail,
                isActiveSubscription: item.isActiveSubscription,
                subscriptionStartDate: item.subscriptionStartDate
                    ? new Date(item.subscriptionStartDate).toLocaleDateString()
                    : "N/A",
                subscriptionEndDate: item.subscriptionEndDate
                    ? new Date(item.subscriptionEndDate).toLocaleDateString()
                    : "N/A",
            }));
            setRows(mappedRows);
        }
    }, [data]);

    const handleRowDoubleClick = (params: GridRowParams) => {
        navigate(`/user?id=${params.row.id}`);
    };

    const handleRowClick = (params: GridRowParams) => {
        if (isMobile) {
            navigate(`/user?id=${params.row.id}`);
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
                const idsToDelete = rows.map((r: Record<string, unknown>) => String(r.id));
                await deleteUsers(idsToDelete);
            } else if (selectedRows) {
                const idsToDelete = Array.from(selectedRows.ids).map(id => id.toString());
                await deleteUsers(idsToDelete);
            }
            setAllSelected(false);
            setSelectedRows(undefined);
        } catch (error) {
            console.error("Error deleting users:", error);
        }
    };

    const hasSelection = !!(selectedRows?.ids.size || allSelected);

    return {
        rows,
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
    };
}
