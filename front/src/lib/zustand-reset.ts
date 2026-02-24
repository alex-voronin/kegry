import { create as actualCreate, StateCreator } from 'zustand';

const storeResetFns = new Set<() => void>();

export const create = (<T>(stateCreator?: StateCreator<T>) => {
  if (stateCreator) {
    const store = actualCreate(stateCreator);
    const initialState = store.getState();
    storeResetFns.add(() => store.setState(initialState, true));
    return store;
  }
  // Curried usage: create<T>()(stateCreator)
  return (sc: StateCreator<T>) => {
    const store = (actualCreate as any)(sc);
    const initialState = store.getState();
    storeResetFns.add(() => store.setState(initialState, true));
    return store;
  };
}) as typeof actualCreate;

export const resetAllStores = () => {
  storeResetFns.forEach((resetFn) => resetFn());
};
