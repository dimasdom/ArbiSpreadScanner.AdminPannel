import { DataGrid, type GridColDef } from '@mui/x-data-grid';
import { useUsers } from "../../hooks/useUsers";

function UsersPage() {
    const {
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
    } = useUsers();

    const columns: GridColDef[] = [
        { field: 'id', headerName: 'User ID', width: 150 },
        { field: 'userMail', headerName: 'Email', width: 200 },
        { field: 'isActiveSubscription', headerName: 'Active Subscription', width: 150, renderCell: (params) => params.value ? 'Yes' : 'No' },
        { field: 'subscriptionStartDate', headerName: 'Start Date', width: 130 },
        { field: 'subscriptionEndDate', headerName: 'End Date', width: 130 },
    ];

    return (
        <div className="shadow-inner mt-6 mx-auto w-full md:max-w-5xl rounded-4xl p-2 md:p-5 bg-white min-h-screen md:min-h-auto relative">
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

export default UsersPage;