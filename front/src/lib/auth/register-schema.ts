import { z } from 'zod';
import { createLoginSchema } from '@/lib/auth/login-schema';

type TFunction = (key: string) => string;

export function createRegisterSchema(t: TFunction) {
  return createLoginSchema(t)
    .extend({
      confirmPassword: z.string().min(1, t('errors.passwordRequired')),
    })
    .refine((data) => data.password === data.confirmPassword, {
      message: t('errors.passwordMismatch'),
      path: ['confirmPassword'],
    });
}
