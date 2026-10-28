using Gley.CameraSystem.Input;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.Input
{
    public class SafeAreaFitterTests
    {
        private GameObject gameObject;
        private RectTransform rectTransform;
        private SafeAreaFitter fitter;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Safe Area", typeof(RectTransform));
            rectTransform = gameObject.GetComponent<RectTransform>();
            fitter = gameObject.AddComponent<SafeAreaFitter>();
        }

        [Test]
        public void SafeAreaInset_AppliedToAnchors()
        {
            fitter.ApplySafeArea(new Rect(100f, 50f, 800f, 500f), new Vector2(1000f, 600f));

            Assert.AreEqual(new Vector2(0.1f, 50f / 600f), rectTransform.anchorMin);
            Assert.AreEqual(new Vector2(0.9f, 550f / 600f), rectTransform.anchorMax);
            Assert.AreEqual(Vector2.zero, rectTransform.offsetMin);
            Assert.AreEqual(Vector2.zero, rectTransform.offsetMax);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
        }
    }
}
