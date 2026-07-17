interface ErrorStateProps {
    message?: string;
    title?: string;
    className?: string;
    onRetry?: () => void;
}

export default function ErrorState({
    message = 'Something went wrong. Please try again later.',
    title = 'Error',
    className = '',
    onRetry,
}: ErrorStateProps) {
    return (
        <div className={`w-full max-w-md ${className}`}>
            <div className="bg-white rounded-2xl shadow-md ring-1 ring-gray-50 ring-inset p-6">
                <div className="flex items-start gap-4">
                    <div className="flex items-center justify-center h-12 w-12 rounded-full bg-red-50 ring-1 ring-red-100">
                        <svg className="h-6 w-6 text-red-600" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                            <path d="M12 9v4m0 4h.01M10.29 3.86l-8.18 14.18A1 1 0 003 19.5h18a1 1 0 00.89-1.46L13.71 3.86a1 1 0 00-1.72 0z" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
                        </svg>
                    </div>

                    <div className="flex-1">
                        <h3 className="text-lg font-semibold text-gray-800">{title}</h3>
                        <p className="mt-2 text-sm text-red-700 bg-red-50 rounded-md px-3 py-2 shadow-md ring-1 ring-red-100 ring-inset">{message}</p>
                        {onRetry && (
                            <button
                                type="button"
                                onClick={onRetry}
                                className="mt-3 text-sm font-medium text-indigo-600 hover:text-indigo-500"
                            >
                                Try again
                            </button>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}
