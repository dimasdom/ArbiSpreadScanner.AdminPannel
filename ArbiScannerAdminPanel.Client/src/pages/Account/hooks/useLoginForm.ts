import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router';
import { useDispatch, useSelector } from 'react-redux';
import type { IRootStore } from '../../../store/store';
import { useLoginMutation } from '../../../store/services/account';
import { clearError } from '../../../store/slices/accountSlice';


interface LoginErrors {
    username: string;
    password: string;
}

export function useLoginForm() {
    const navigate = useNavigate();
    const dispatch = useDispatch();
    const [login, { isLoading: loading }] = useLoginMutation();

    const isLoggedIn = useSelector((state: IRootStore) => state.account.isLoggedIn);
    const loginError = useSelector((state: IRootStore) => state.account.error);

    const [errors, setErrors] = useState<LoginErrors>({ username: '', password: '' });

    useEffect(() => { dispatch(clearError()); }, [dispatch]);
    useEffect(() => { if (isLoggedIn) navigate('/'); }, [isLoggedIn, navigate]);

    const clearFieldError = (field: keyof LoginErrors) =>
        setErrors((prev) => ({ ...prev, [field]: '' }));

    const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        const usernameVal = (data.get('username') as string) ?? '';
        const passwordVal = (data.get('password') as string) ?? '';

        const usernameErr = !usernameVal || usernameVal.length < 3
            ? 'Username must be at least 3 characters long.'
            : '';
        const passwordErr = !passwordVal || passwordVal.length < 8
            ? 'Password must be at least 8 characters long.'
            : '';

        if (usernameErr || passwordErr) {
            setErrors({ username: usernameErr, password: passwordErr });
            return;
        }

        login({ userName: usernameVal, password: passwordVal });
    };

    return { errors, loading, loginError, clearFieldError, handleSubmit };
}
