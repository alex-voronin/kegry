declare module '*.module.css' {
  const classes: { [key: string]: string };
  export default classes;
}

declare interface Window {
  Tawk_API?: any;
  Tawk_LoadStart?: Date;
}
