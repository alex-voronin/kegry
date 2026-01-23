import Image from 'next/image';
import React from 'react';

import { cn } from '@/lib/utils';

interface IconProps {
  src?: string;
  alt?: string;
  width?: number;
  height?: number;
  priority?: boolean;
  className?: string;
  disabled?: boolean;
  onClick?: () => void;
  children?: React.ReactNode;
}

export function Icon({
  src,
  alt,
  width,
  height,
  priority,
  className,
  disabled,
  onClick,
  children,
}: IconProps) {
  return (
    <div
      onClick={disabled ? undefined : onClick}
      className={cn(
        'relative hover:brightness-75 active:brightness-75',
        `w-[${width}px] h-[${height}px]`,
        disabled && 'pointer-events-none opacity-30',
        className,
      )}
    >
      {src && alt && width && height && (
        <Image
          className="pointer-events-none h-full w-full select-none"
          src={src}
          alt={alt}
          width={width}
          height={height}
          priority={priority}
        />
      )}
      {children}
    </div>
  );
}
