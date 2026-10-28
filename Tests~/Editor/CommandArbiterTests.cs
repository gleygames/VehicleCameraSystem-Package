using System.Collections.Generic;
using NUnit.Framework;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class CommandArbiterTests
    {
        private readonly List<int> endedCommandIds = new List<int>();
        private readonly List<CommandEndResult> endedResults = new List<CommandEndResult>();

        private CommandArbiter arbiter;

        [SetUp]
        public void SetUp()
        {
            endedCommandIds.Clear();
            endedResults.Clear();
            arbiter = new CommandArbiter();
            arbiter.CommandEnded += RecordCommandEnded;
        }

        [Test]
        public void NewCommand_ReplacesRunning_RaisesReplaced()
        {
            int first = arbiter.BeginCommand(CommandKind.PointOfInterest, false);
            int second = arbiter.BeginCommand(CommandKind.ViewSwitch, false);

            Assert.Greater(first, 0);
            Assert.AreNotEqual(first, second);
            Assert.AreEqual(1, endedCommandIds.Count);
            Assert.AreEqual(first, endedCommandIds[0]);
            Assert.AreEqual(CommandEndResult.Replaced, endedResults[0]);
            Assert.AreEqual(second, arbiter.RunningCommandId);
            Assert.AreEqual(CommandKind.ViewSwitch, arbiter.RunningKind);

            arbiter.EndCommand(first, CommandEndResult.Completed);
            Assert.AreEqual(1, endedCommandIds.Count);
            arbiter.EndCommand(second, CommandEndResult.Completed);
            Assert.AreEqual(2, endedCommandIds.Count);
            Assert.AreEqual(second, endedCommandIds[1]);
            Assert.AreEqual(CommandEndResult.Completed, endedResults[1]);
            Assert.IsFalse(arbiter.HasRunningCommand);
        }

        [Test]
        public void ReplacedLockingCommand_LockEndsUnlessNewLocks()
        {
            arbiter.BeginCommand(CommandKind.PointOfInterest, true);
            Assert.IsTrue(arbiter.IsPlayerControlLocked);

            arbiter.BeginCommand(CommandKind.ViewSwitch, false);
            Assert.IsFalse(arbiter.IsPlayerControlLocked);

            arbiter.BeginCommand(CommandKind.PointOfInterest, true);
            int lockingCommand = arbiter.BeginCommand(CommandKind.Reset, true);
            Assert.IsTrue(arbiter.IsPlayerControlLocked);

            arbiter.EndCommand(lockingCommand, CommandEndResult.Completed);
            Assert.IsFalse(arbiter.IsPlayerControlLocked);
        }

        [Test]
        public void ExplicitLock_SurvivesCommandReplacement()
        {
            arbiter.SetExplicitPlayerLock(true);
            arbiter.BeginCommand(CommandKind.PointOfInterest, true);
            int unlockedCommand = arbiter.BeginCommand(CommandKind.ViewSwitch, false);
            Assert.IsTrue(arbiter.IsPlayerControlLocked);

            arbiter.EndCommand(unlockedCommand, CommandEndResult.Completed);
            Assert.IsTrue(arbiter.IsPlayerControlLocked);
            Assert.IsTrue(arbiter.IsExplicitPlayerLockSet);

            arbiter.SetExplicitPlayerLock(false);
            Assert.IsFalse(arbiter.IsPlayerControlLocked);
        }

        [Test]
        public void PlayerInputWhileUnlocked_InterruptsRunning()
        {
            Assert.IsTrue(arbiter.AcceptPlayerInput());
            Assert.AreEqual(0, endedCommandIds.Count);

            int commandId = arbiter.BeginCommand(CommandKind.PointOfInterest, false);
            Assert.IsTrue(arbiter.AcceptPlayerInput());

            Assert.AreEqual(1, endedCommandIds.Count);
            Assert.AreEqual(commandId, endedCommandIds[0]);
            Assert.AreEqual(CommandEndResult.Interrupted, endedResults[0]);
            Assert.IsFalse(arbiter.HasRunningCommand);
        }

        [Test]
        public void PlayerInputWhileLocked_Rejected()
        {
            int commandId = arbiter.BeginCommand(CommandKind.PointOfInterest, true);
            Assert.IsFalse(arbiter.AcceptPlayerInput());
            Assert.AreEqual(0, endedCommandIds.Count);
            Assert.AreEqual(commandId, arbiter.RunningCommandId);

            arbiter.EndCommand(commandId, CommandEndResult.Completed);
            arbiter.SetExplicitPlayerLock(true);
            Assert.IsFalse(arbiter.AcceptPlayerInput());

            int unlockedCommand = arbiter.BeginCommand(CommandKind.ViewSwitch, false);
            Assert.IsFalse(arbiter.AcceptPlayerInput());
            Assert.AreEqual(unlockedCommand, arbiter.RunningCommandId);
            Assert.AreEqual(1, endedCommandIds.Count);
        }

        [TearDown]
        public void TearDown()
        {
            arbiter.CommandEnded -= RecordCommandEnded;
        }

        private void RecordCommandEnded(int commandId, CommandEndResult result)
        {
            endedCommandIds.Add(commandId);
            endedResults.Add(result);
        }
    }
}
