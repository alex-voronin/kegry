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
  stopPropagation?: boolean;
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
  stopPropagation,
  onClick,
  children,
}: IconProps) {
  const handleClick = (e: React.MouseEvent) => {
    if (disabled) return;
    if (stopPropagation) {
      e.stopPropagation();
    }
    onClick?.();
  };

  return (
    <div
      onClick={handleClick}
      className={cn(
        'relative hover:brightness-75 active:brightness-75',
        disabled && 'pointer-events-none opacity-30',
        className,
      )}
      style={{ width, height }}
    >
      {src && alt && width && height && (
        <Image
          className="pointer-events-none h-full w-full object-contain select-none"
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
