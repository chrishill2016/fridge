// Thin IndexedDB wrapper used by IndexedDbLocalStore.cs. Values use out-of-line keys.
const DB_NAME = 'fridge-chef';
const DB_VERSION = 1;
const STORES = ['settings', 'shoppingList', 'savedRecipes'];

let dbPromise;

function openDb() {
    dbPromise ??= new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_NAME, DB_VERSION);
        request.onupgradeneeded = () => {
            const db = request.result;
            for (const store of STORES) {
                if (!db.objectStoreNames.contains(store)) {
                    db.createObjectStore(store);
                }
            }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
    return dbPromise;
}

async function run(store, mode, action) {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(store, mode);
        const request = action(tx.objectStore(store));
        tx.oncomplete = () => resolve(request.result);
        tx.onerror = () => reject(tx.error);
        tx.onabort = () => reject(tx.error);
    });
}

export async function get(store, key) {
    return (await run(store, 'readonly', s => s.get(key))) ?? null;
}

export function getAll(store) {
    return run(store, 'readonly', s => s.getAll());
}

export async function put(store, key, value) {
    await run(store, 'readwrite', s => s.put(value, key));
}

export async function remove(store, key) {
    await run(store, 'readwrite', s => s.delete(key));
}

export async function clear(store) {
    await run(store, 'readwrite', s => s.clear());
}
