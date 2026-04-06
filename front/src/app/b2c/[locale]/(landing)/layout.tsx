import styles from './layout.module.css';

export default function LandingLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  // Landing page uses full-width layout
  // This wrapper breaks out of the parent container's max-width constraint
  return (
    <div id="landing-scroll-container" className={styles.wrapper}>
      {children}
    </div>
  );
}
