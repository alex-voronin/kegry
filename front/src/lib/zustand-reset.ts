import { create as actualCreate, StateCreator } from 'zustand';

const storeResetFns = new Set<() => void>();

export const create = (<T>(stateCreator: StateCreator<T>) => {
  const store = actualCreate(stateCreator);
  storeResetFns.add(() => store.setState(store.getInitialState(), true));
  return store;
}) as typeof actualCreate;

export const resetAllStores = () => {
  storeResetFns.forEach((resetFn) => resetFn());
};
