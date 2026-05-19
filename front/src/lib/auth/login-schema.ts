import { z } from 'zod';

type TFunction = (key: string) => string;

export function createLoginSchema(t: TFunction) {
  return z.object({
    email: z.email(t('errors.emailInvalid')).min(1, t('errors.emailRequired')),
    password: z
      .string()
      .min(1, t('errors.passwordRequired'))
      .min(10, t('errors.passwordShort'))
      .refine(
        (pw) => /[A-Z]/.test(pw) && /[a-z]/.test(pw) && /[0-9]/.test(pw),
        {
          message: t('errors.passwordComplexity'),
        },
      ),
  });
}
