'use client';

import ReactSelect, { Props as SelectProps, GroupBase } from 'react-select';
import { cn } from '@/lib/utils';

export interface SelectOption {
  value: string | number;
  label: string;
}

const selectStyles = {
  control: (base: any, state: { isFocused: boolean }) => ({
    ...base,
    backgroundColor: '#292929',
    borderColor: state.isFocused ? '#ffffff' : 'transparent',
    minHeight: '48px',
    borderRadius: '0',
    boxShadow: 'none',
    padding: '0 4px',
    '@media (min-width: 500px)': {
      borderRadius: '12px',
    },
    '&:hover': {
      borderColor: state.isFocused ? '#ffffff' : 'rgba(255,255,255,0.2)',
    },
  }),
  menu: (base: any) => ({
    ...base,
    backgroundColor: '#292929',
    borderRadius: '12px',
    marginTop: '4px',
    overflow: 'hidden',
    zIndex: 50,
  }),
  option: (base: any, state: any) => ({
    ...base,
    backgroundColor: state.isSelected
      ? 'rgba(255,255,255,0.15)'
      : state.isFocused
        ? 'rgba(255,255,255,0.1)'
        : 'transparent',
    color: '#ffffff',
    cursor: 'pointer',
    '&:active': {
      backgroundColor: 'rgba(255,255,255,0.2)',
    },
  }),
  singleValue: (base: any) => ({
    ...base,
    color: '#ffffff',
    fontSize: '16px',
  }),
  input: (base: any) => ({
    ...base,
    color: '#ffffff',
  }),
  placeholder: (base: any) => ({
    ...base,
    color: '#A0A0A0',
    fontSize: '16px',
  }),
  indicatorSeparator: () => ({
    display: 'none',
  }),
  dropdownIndicator: (base: any) => ({
    ...base,
    color: '#A0A0A0',
    '&:hover': {
      color: '#ffffff',
    },
  }),
};

interface CustomSelectProps<
  Option = SelectOption,
  IsMulti extends boolean = false,
  Group extends GroupBase<Option> = GroupBase<Option>,
> extends SelectProps<Option, IsMulti, Group> {
  className?: string;
  error?: string;
}

export function Select<
  Option = SelectOption,
  IsMulti extends boolean = false,
  Group extends GroupBase<Option> = GroupBase<Option>,
>({ className, error, ...props }: CustomSelectProps<Option, IsMulti, Group>) {
  return (
    <div className={cn('flex w-full flex-col gap-1', className)}>
      <ReactSelect styles={selectStyles} {...props} />
      {error && <p className="text-destructive mt-1 text-sm">{error}</p>}
    </div>
  );
}
