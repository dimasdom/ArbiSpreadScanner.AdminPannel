import { DataGrid, type GridColDef } from '@mui/x-data-grid';
import { useSubscriptions } from "../../hooks/useSubscriptions";

function SubscriptionsPage() {
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
        handleCreate,
    } = useSubscriptions();

    const columns: GridColDef[] = [
        { field: 'id', headerName: 'ID', width: 100 },
        { field: 'type', headerName: 'Type', width: 120 },
        { field: 'price', headerName: 'Price', width: 100 },
        { field: 'durationInDays', headerName: 'Duration (Days)', width: 130 },
    ];

    return (
        <div className="shadow-inner mt-6 mx-auto w-full md:max-w-5xl rounded-4xl p-2 md:p-5 bg-white min-h-screen md:min-h-auto relative">
            <div className="mb-4 flex gap-2">
                <button
                    onClick={handleCreate}
                    className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-4 rounded-lg transition-colors"
                >
                    Create
                </button>
                {hasSelection ? (
                    <button
                        onClick={handleDeleteSelected}
                        className="bg-red-600 hover:bg-red-700 text-white font-medium py-2 px-4 rounded-lg transition-colors"
                    >
                        Delete Selected ({allSelected ? "All" : selectedRows?.ids.size})
                    </button>
                ) : null}
            </div>
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

export default SubscriptionsPage;