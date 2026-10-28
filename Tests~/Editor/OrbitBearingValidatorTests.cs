using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class OrbitBearingValidatorTests
    {
        [Test]
        public void FindAmbiguousBearings_Rectangle_None()
        {
            Vector3[] corners =
            {
                new Vector3(-2f, 0f, -5f),
                new Vector3(2f, 0f, -5f),
                new Vector3(2f, 0f, 5f),
                new Vector3(-2f, 0f, 5f)
            };
            OrbitBearing bearing = CreatePolygonBearing(corners);
            OrbitBearingValidator validator = new OrbitBearingValidator();

            Assert.AreEqual(0, validator.FindAmbiguousBearings(bearing).Count);
        }

        [Test]
        public void FindAmbiguousBearings_LShape_NonEmpty()
        {
            Vector3[] corners =
            {
                new Vector3(-4f, 0f, -4f),
                new Vector3(4f, 0f, -4f),
                new Vector3(4f, 0f, -2f),
                new Vector3(-2f, 0f, -2f),
                new Vector3(-2f, 0f, 4f),
                new Vector3(-4f, 0f, 4f)
            };
            OrbitBearing bearing = CreatePolygonBearing(corners);
            OrbitBearingValidator validator = new OrbitBearingValidator();
            IReadOnlyList<float> ambiguous = validator.FindAmbiguousBearings(bearing);

            Assert.Greater(ambiguous.Count, 0);
            bool includesRear = false;
            for (int index = 0; index < ambiguous.Count; index++)
            {
                if (ambiguous[index] == 0f)
                {
                    includesRear = true;
                    break;
                }
            }

            Assert.IsTrue(includesRear);
        }

        private OrbitBearing CreatePolygonBearing(IReadOnlyList<Vector3> corners)
        {
            List<Vector3> positions = new List<Vector3>();
            List<float> distances = new List<float>();
            float length = 0f;

            for (int index = 0; index < corners.Count; index++)
            {
                positions.Add(corners[index]);
                distances.Add(length);
                length += Vector3.Distance(corners[index], corners[(index + 1) % corners.Count]);
            }

            positions.Add(corners[0]);
            distances.Add(length);
            OrbitBearing bearing = new OrbitBearing();
            bearing.Rebuild(positions, distances, length);
            return bearing;
        }
    }
}
