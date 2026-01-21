import styles from './spinner.module.css';
import { cn } from '@/lib/utils';

interface SpinnerProps {
  className?: string;
}

export const Spinner = ({ className }: SpinnerProps) => {
  return <div className={cn(styles.spinner, className)} />;
};
