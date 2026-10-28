using NUnit.Framework;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class HeldInputGateTests
    {
        [Test]
        public void AfterRearm_HeldValueIgnoredUntilNeutral()
        {
            HeldInputGate gate = new HeldInputGate();
            gate.ReceiveHeldInput(1f, 0f, -0.5f);
            Assert.AreEqual(1f, gate.Horizontal);
            Assert.AreEqual(-0.5f, gate.Zoom);

            gate.Rearm();
            Assert.AreEqual(0f, gate.Horizontal);
            Assert.AreEqual(0f, gate.Zoom);
            Assert.IsTrue(gate.IsNeutral);

            gate.ReceiveHeldInput(1f, 0.8f, -0.5f);
            Assert.AreEqual(0f, gate.Horizontal);
            Assert.AreEqual(0.8f, gate.Vertical);
            Assert.AreEqual(0f, gate.Zoom);

            gate.ReceiveHeldInput(0.6f, 0.8f, -0.5f);
            Assert.AreEqual(0f, gate.Horizontal);
            Assert.AreEqual(0.6f, gate.ReceivedHorizontal);

            gate.ReceiveHeldInput(0.005f, 0.8f, -0.5f);
            Assert.AreEqual(0.005f, gate.Horizontal);
            Assert.AreEqual(0f, gate.Zoom);

            gate.ReceiveHeldInput(1f, 0.8f, 0f);
            Assert.AreEqual(1f, gate.Horizontal);
            Assert.AreEqual(0f, gate.Zoom);

            gate.ReceiveHeldInput(1f, 0.8f, -0.5f);
            Assert.AreEqual(1f, gate.Horizontal);
            Assert.AreEqual(0.8f, gate.Vertical);
            Assert.AreEqual(-0.5f, gate.Zoom);
            Assert.IsFalse(gate.IsNeutral);
        }
    }
}
