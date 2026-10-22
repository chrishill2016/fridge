// Browser hooks for ConnectivityService.cs and AppUpdateService.cs.
let connectivityHandler;

export function watchConnectivity(dotnetRef) {
    unwatchConnectivity();
    connectivityHandler = () => dotnetRef.invokeMethodAsync('OnConnectivityChanged', navigator.onLine);
    window.addEventListener('online', connectivityHandler);
    window.addEventListener('offline', connectivityHandler);
    return navigator.onLine;
}

export function unwatchConnectivity() {
    if (connectivityHandler) {
        window.removeEventListener('online', connectivityHandler);
        window.removeEventListener('offline', connectivityHandler);
        connectivityHandler = undefined;
    }
}

const UPDATE_CHECK_INTERVAL_MS = 60 * 60 * 1000;

// Returns straight away; `ready` never resolves if no worker registers (e.g. unsupported browser).
export function watchForUpdates(dotnetRef) {
    if (!('serviceWorker' in navigator)) {
        return;
    }

    navigator.serviceWorker.ready.then(registration => {
        const notify = () => dotnetRef.invokeMethodAsync('OnUpdateAvailable');

        // Only an update if a worker already controls the page; the first install is not.
        if (registration.waiting && navigator.serviceWorker.controller) {
            notify();
        }

        registration.addEventListener('updatefound', () => {
            const worker = registration.installing;
            worker?.addEventListener('statechange', () => {
                if (worker.state === 'installed' && navigator.serviceWorker.controller) {
                    notify();
                }
            });
        });

        setInterval(() => registration.update().catch(() => { }), UPDATE_CHECK_INTERVAL_MS);
    });
}

export async function applyUpdate() {
    const registration = await navigator.serviceWorker?.getRegistration();
    if (registration?.waiting) {
        navigator.serviceWorker.addEventListener('controllerchange', () => location.reload(), { once: true });
        registration.waiting.postMessage({ type: 'SKIP_WAITING' });
    } else {
        location.reload();
    }
}
