import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import type { GridRowParams, GridRowSelectionModel } from "@mui/x-data-grid";
import { getPaymentStatus, type PaymentModel } from "../types/accountType";
import { useGetPaymentsQuery, useRemovePaymentsMutation } from "../store/services/payments";

export function usePayments() {
    const navigate = useNavigate();
    const [rows, setRows] = useState<Record<string, unknown>[]>([]);
    const [isMobile, setIsMobile] = useState(window.innerWidth < 768);
    const [paginationModel, setPaginationModel] = useState({ pageSize: 10, page: 1 });
    const [selectedRows, setSelectedRows] = useState<GridRowSelectionModel | undefined>(undefined);
    const [allSelected, setAllSelected] = useState<boolean>(false);

    const { data, isLoading } = useGetPaymentsQuery(paginationModel.page);
    const [removePayments] = useRemovePaymentsMutation();

    useEffect(() => {
        const handleResize = () => setIsMobile(window.innerWidth < 768);
        window.addEventListener("resize", handleResize);
        return () => window.removeEventListener("resize", handleResize);
    }, []);

    useEffect(() => {
        const result = data ?? { isSuccess: false, isFailed: true, errors: [], reasons: [], value: [] };
        if (result.isSuccess && result.value) {
            const mappedRows = result.value.map((item: PaymentModel) => ({
                id: item.id,
                userId: item.userId,
                amount: item.amount.toFixed(2),
                paymentDate: new Date(item.paymentDate).toLocaleDateString(),
                status: getPaymentStatus(item.status),
                transactionId: item.transactionId,
            }));
            setRows(mappedRows);
        }
    }, [data]);

    const handleRowDoubleClick = (params: GridRowParams) => {
        navigate(`/payment?id=${params.row.id}`);
    };

    const handleRowClick = (params: GridRowParams) => {
        if (isMobile) {
            navigate(`/payment?id=${params.row.id}`);
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
                const idsToDelete = rows.map((row: Record<string, unknown>) => Number(row.id));
                await removePayments(idsToDelete);
            } else if (selectedRows) {
                const idsToDelete = Array.from(selectedRows.ids).map(Number);
                await removePayments(idsToDelete);
            }
            setAllSelected(false);
            setSelectedRows(undefined);
        } catch (error) {
            console.error("Error deleting payments:", error);
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
