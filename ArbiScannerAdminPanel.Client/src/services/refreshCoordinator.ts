/**
 * Shared refresh coordinator — ensures only one token-refresh call is
 * in-flight at any time, regardless of how many requests trigger a 401.
 *
 * Any caller that arrives while a refresh is already in progress is queued
 * and will receive the resolved result once the in-flight refresh completes.
 */

type PendingEntry = {
    resolve: (token: string) => void;
    reject: (error: unknown) => void;
};

let isRefreshing = false;
let pendingQueue: PendingEntry[] = [];

function flushQueue(error: unknown, token: string | null): void {
    pendingQueue.forEach(({ resolve, reject }) => {
        if (error != null || token == null) {
            reject(error);
        } else {
            resolve(token);
        }
    });
    pendingQueue = [];
}

/**
 * Runs `doRefresh` exactly once while concurrent callers wait in a queue.
 * Throws (and propagates to all queued callers) on failure.
 */
export async function coordinatedRefresh(doRefresh: () => Promise<string>): Promise<string> {
    if (isRefreshing) {
        return new Promise<string>((resolve, reject) => {
            pendingQueue.push({ resolve, reject });
        });
    }

    isRefreshing = true;
    try {
        const token = await doRefresh();
        flushQueue(null, token);
        return token;
    } catch (err) {
        flushQueue(err, null);
        throw err;
    } finally {
        isRefreshing = false;
    }
}
