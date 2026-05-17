import type { AxiosRequestConfig } from 'axios';

export function paramsForReq(
  params: Record<
    string,
    string | number | boolean | (string | number | boolean)[] | undefined | null
  >,
): Pick<AxiosRequestConfig, 'params' | 'paramsSerializer'> {
  return {
    params,
    paramsSerializer: {
      serialize: (p) => {
        const sp = new URLSearchParams();
        for (const [key, val] of Object.entries(p)) {
          if (Array.isArray(val)) {
            val.forEach((v) => sp.append(key, String(v)));
          } else if (val !== undefined && val !== null) {
            sp.set(key, String(val));
          }
        }
        return sp.toString();
      },
    },
  };
}
