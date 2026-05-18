let getToken: () => string | null = () => null;

export const setTokenGetter = (getter: () => string | null) => {
    getToken = getter;
};

export const getAccessToken = (): string | null => getToken();
