interface DeleteSelectedButtonProps {
    onClick: () => void;
    allSelected: boolean;
    count?: number;
}

export default function DeleteSelectedButton({ onClick, allSelected, count }: Readonly<DeleteSelectedButtonProps>) {
    return (
        <button
            type="button"
            onClick={onClick}
            className="bg-red-600 hover:bg-red-700 text-white font-medium py-2 px-4 rounded-lg transition-colors"
        >
            Delete Selected ({allSelected ? "All" : count})
        </button>
    );
}
