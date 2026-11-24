using DatastoresDX.Runtime;
using UnityEngine.InputSystem;

namespace BetterInputSystem.Runtime
{
    /// <summary>
    /// A gameplay action map, looked up by its Datastores Uid. Enabling it records a request; the map only runs
    /// while BetterInput is not in UI mode.
    /// </summary>
    public sealed class BetterActionMap
    {
        private readonly BetterInput m_owner;

        public Uid Id { get; }
        public InputActionMap Actions { get; }

        /// <summary>What game code last asked for.</summary>
        public bool IsRequested { get; private set; }

        /// <summary>Whether the map is actually running, after the UI mode gate.</summary>
        public bool IsEnabled => Actions.enabled;

        internal BetterActionMap(BetterInput owner, Uid id, InputActionMap actions)
        {
            m_owner = owner;
            Id = id;
            Actions = actions;
        }

        public void Enable(bool enable)
        {
            IsRequested = enable;
            m_owner.ApplyGate(this);
        }
    }
}
