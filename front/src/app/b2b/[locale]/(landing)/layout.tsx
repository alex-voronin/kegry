import styles from './layout.module.css';

export default function LandingLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div id="landing-scroll-container" className={styles.wrapper}>
      {children}
    </div>
  );
}
