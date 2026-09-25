using System;

namespace Gley.CameraSystem
{
    public class CommandArbiter
    {
        private CommandKind runningKind;
        private int nextCommandId = 1;
        private int runningCommandId;
        private bool runningHoldsLock;
        private bool isExplicitPlayerLockSet;

        public event Action<int, CommandEndResult> CommandEnded;

        public HeldInputGate HeldInput { get; }
        public CommandKind RunningKind => runningKind;
        public int RunningCommandId => runningCommandId;
        public bool HasRunningCommand => runningCommandId != 0;
        public bool IsExplicitPlayerLockSet => isExplicitPlayerLockSet;
        public bool IsPlayerControlLocked => isExplicitPlayerLockSet || (runningCommandId != 0 && runningHoldsLock);

        public CommandArbiter()
        {
            HeldInput = new HeldInputGate();
        }

        public int BeginCommand(CommandKind kind, bool holdsLock)
        {
            EndRunningCommand(CommandEndResult.Replaced);
            runningKind = kind;
            runningCommandId = nextCommandId;
            runningHoldsLock = holdsLock;
            if (nextCommandId == int.MaxValue)
            {
                nextCommandId = 1;
            }
            else
            {
                nextCommandId++;
            }

            HeldInput.Rearm();
            return runningCommandId;
        }

        public void EndCommand(int id, CommandEndResult result)
        {
            if (id == 0 || id != runningCommandId)
            {
                return;
            }

            runningCommandId = 0;
            runningHoldsLock = false;
            CommandEnded?.Invoke(id, result);
        }

        public void EndRunningCommand(CommandEndResult result)
        {
            EndCommand(runningCommandId, result);
        }

        public void SetExplicitPlayerLock(bool locked)
        {
            isExplicitPlayerLockSet = locked;
            if (locked)
            {
                HeldInput.Rearm();
            }
        }

        public bool AcceptPlayerInput()
        {
            if (IsPlayerControlLocked)
            {
                return false;
            }

            EndRunningCommand(CommandEndResult.Interrupted);
            return true;
        }
    }
}
