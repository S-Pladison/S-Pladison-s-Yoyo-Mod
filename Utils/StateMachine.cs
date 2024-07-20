using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SPYoyoMod.Utils
{
    public sealed class StateMachine<T> where T : Enum
    {
        public interface IStateBehaviour
        {
            IStateBehaviour OnEnter(Action action);
            IStateBehaviour Process(Action<StateMachine<T>> action);
            IStateBehaviour OnExit(Action action);
        }

        private class StateBehaviour : IStateBehaviour
        {
            private event Action OnEnterEvt;
            private event Action<StateMachine<T>> ProcessEvt;
            private event Action OnExitEvt;

            public IStateBehaviour OnEnter(Action action)
            {
                OnEnterEvt += action;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnEnterInvoke() => OnEnterEvt?.Invoke();

            public IStateBehaviour OnExit(Action action)
            {
                OnExitEvt += action;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnExitInvoke() => OnExitEvt?.Invoke();

            public IStateBehaviour Process(Action<StateMachine<T>> action)
            {
                ProcessEvt += action;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ProcessInvoke(StateMachine<T> machine) => ProcessEvt?.Invoke(machine);
        }

        public event Action OnStateChanged;
        public event Action OnPreProcess;

        private readonly Dictionary<T, StateBehaviour> _states = [];
        private StateBehaviour _currentBehaviour;

        public T CurrentState { get; private set; }

        public IStateBehaviour RegisterState(T state)
        {
            var stateBehaviour = new StateBehaviour();
            _states[state] = stateBehaviour;
            return stateBehaviour;
        }

        public void SetState(T state)
        {
            if (!_states.TryGetValue(state, out StateBehaviour behaviour))
                throw new InvalidOperationException($"...");

            if (CurrentState.Equals(state) && _currentBehaviour != null)
                return;

            _currentBehaviour?.OnExitInvoke();
            CurrentState = state;
            _currentBehaviour = behaviour;
            _currentBehaviour.OnEnterInvoke();

            OnStateChanged?.Invoke();
        }

        public void Process()
        {
            if (_currentBehaviour is null)
                throw new InvalidOperationException($"...");

            OnPreProcess?.Invoke();

            _currentBehaviour.ProcessInvoke(this);
        }
    }
}