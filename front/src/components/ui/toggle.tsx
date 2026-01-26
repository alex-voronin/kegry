import { Switch } from '@headlessui/react';

interface ToggleProps {
  checked: boolean;
  onChange: (checked: boolean) => void;
  srTitle?: string;
}

export function Toggle({ checked, onChange, srTitle }: ToggleProps) {
  return (
    <Switch
      checked={checked}
      onChange={onChange}
      className={`${
        checked ? 'bg-white' : 'bg-white/40'
      } relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none`}
    >
      <span className="sr-only">{srTitle || 'Enable setting'}</span>
      <span
        className={`${
          checked ? 'translate-x-6' : 'translate-x-1'
        } bg-surface inline-block h-4 w-4 transform rounded-full transition-transform`}
      />
    </Switch>
  );
}
