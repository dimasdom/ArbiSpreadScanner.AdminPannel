import { DataGrid, type GridColDef } from '@mui/x-data-grid';
import { useUserSubscriptions } from "../../hooks/useUserSubscriptions";
import ErrorState from "../../components/ErrorState";

function UserSubscriptionsPage() {
    const {
        rows,
        paginationModel,
        selectedRows,
        allSelected,
        isLoading,
        isError,
        hasSelection,
        setPaginationModel,
        handleRowDoubleClick,
        handleRowClick,
        handleRowSelectionChange,
        handleDeleteSelected,
        handleCreate,
    } = useUserSubscriptions();

    if (isError) {
        return (
            <div className="mt-6 mx-auto w-full md:max-w-5xl flex justify-center">
                <ErrorState message="Failed to load user subscriptions. Please try again later." />
            </div>
        );
    }

    const columns: GridColDef[] = [
        { field: 'id', headerName: 'ID', width: 100 },
        { field: 'userMail', headerName: 'User Mail', width: 200 },
        { field: 'subcriptionType', headerName: 'Subscription Type', width: 150 },
        { field: 'subscriptionStartDate', headerName: 'Start Date', width: 150 },
        { field: 'subscriptionEndDate', headerName: 'End Date', width: 150 },
    ];

    return (
        <div className="shadow-inner mt-6 mx-auto w-full md:max-w-5xl rounded-4xl p-2 md:p-5 bg-white min-h-screen md:min-h-auto relative">
            <button
                    onClick={handleCreate}
                    className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-4 rounded-lg transition-colors"
                >
                    Create
                </button>
            {hasSelection ? (
                <div className="mb-4 flex gap-2">
                    <button
                        onClick={handleDeleteSelected}
                        className="bg-red-600 hover:bg-red-700 text-white font-medium py-2 px-4 rounded-lg transition-colors"
                    >
                        Delete Selected ({allSelected ? "All" : selectedRows?.ids.size})
                    </button>
                </div>
            ) : <></>}
            <div className="rounded-4xl shadow-2xl w-full px-2 md:px-5 py-5">
                <DataGrid
                    rows={rows}
                    columns={columns}
                    pageSizeOptions={[10]}
                    paginationModel={paginationModel}
                    onPaginationModelChange={setPaginationModel}
                    loading={isLoading}
                    checkboxSelection
                    disableRowSelectionOnClick
                    onRowSelectionModelChange={handleRowSelectionChange}
                    rowSelectionModel={selectedRows}
                    onRowDoubleClick={handleRowDoubleClick}
                    onRowClick={handleRowClick}
                />
            </div>
        </div>

    );
}

export default UserSubscriptionsPage;