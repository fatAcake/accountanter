const apiUrl = (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, '') ?? '';

export const env = {
  apiUrl,
  appTitle: (import.meta.env.VITE_APP_TITLE as string | undefined) || 'Accountent',
  timezone: (import.meta.env.VITE_TIMEZONE as string | undefined) || 'Asia/Krasnoyarsk',
} as const;
