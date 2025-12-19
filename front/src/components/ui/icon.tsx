import Image from 'next/image';
import React from 'react';

interface IconProps {
  src?: string;
  alt?: string;
  width?: number;
  height?: number;
  priority?: boolean;
  onClick?: () => void;
  children?: React.ReactNode;
}

export function Icon({
  src,
  alt,
  width,
  height,
  priority,
  onClick,
  children,
}: IconProps) {
  return (
    <div
      onClick={onClick}
      className="relative hover:brightness-75 active:brightness-75"
    >
      {src && alt && width && height && (
        <Image
          className="pointer-events-none select-none"
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
