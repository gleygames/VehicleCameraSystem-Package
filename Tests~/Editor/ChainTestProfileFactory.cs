using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    internal class ChainTestProfileFactory
    {
        private const float MaximumCornerRadius = 1.5f;
        private const float ArcHandleScale = 0.55228475f;
        private const float HitchGap = 0.5f;
        private const int RoundedKnotCount = 8;

        private readonly List<Object> objects = new List<Object>();

        public VehicleProfile CreateProfile(string orbitName, float width, float depth)
        {
            VehicleProfile profile = ScriptableObject.CreateInstance<VehicleProfile>();
            objects.Add(profile);
            VehicleOrbit orbit = profile.AddOrbit(orbitName);
            Vector3 frontLeft = Vector3.zero;
            Vector3 frontRight = new Vector3(width, 0f, 0f);
            Vector3 rearRight = new Vector3(width, 0f, depth);
            Vector3 rearLeft = new Vector3(0f, 0f, depth);
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            knots.Add(CreateKnot(frontLeft, rearLeft, frontRight));
            knots.Add(CreateKnot(frontRight, frontLeft, rearRight));
            knots.Add(CreateKnot(rearRight, frontRight, rearLeft));
            knots.Add(CreateKnot(rearLeft, rearRight, frontLeft));
            orbit.Configure(knots, Quaternion.identity);
            float length = 2f * (width + depth);
            OrbitRemovableSection front = new OrbitRemovableSection();
            front.Configure(0f, width / length);
            OrbitRemovableSection rear = new OrbitRemovableSection();
            rear.Configure((width + depth) / length, (2f * width + depth) / length);
            orbit.ConfigureRemovableSections(front, rear);
            profile.AddWatchMarker(orbit, "Right", (width + depth * 0.5f) / length, new Vector3(width * 0.5f, 2f, depth * 0.5f), true);
            profile.AddWatchMarker(orbit, "Left", (2f * width + depth * 1.5f) / length, new Vector3(width * 0.5f, 3f, depth * 0.5f), true);
            VehicleConnectorAnchors anchors = new VehicleConnectorAnchors();
            anchors.Configure(new Vector3(width * 0.5f, 0f, 0f), new Vector3(width * 0.5f, 0f, depth + 2f));
            profile.ConfigureConnectorAnchors(anchors);
            return profile;
        }

        public VehicleProfile CreateRoundedProfile(string orbitName, float halfWidth, float front, float rear)
        {
            VehicleProfile profile = ScriptableObject.CreateInstance<VehicleProfile>();
            objects.Add(profile);
            VehicleOrbit orbit = profile.AddOrbit(orbitName);
            float radius = Mathf.Min(MaximumCornerRadius, halfWidth * 0.5f);
            Vector3[] corners =
            {
                new Vector3(halfWidth, 0f, -rear),
                new Vector3(-halfWidth, 0f, -rear),
                new Vector3(-halfWidth, 0f, front),
                new Vector3(halfWidth, 0f, front)
            };
            Vector3[] anchors =
            {
                new Vector3(halfWidth, 0f, -rear + radius),
                new Vector3(halfWidth - radius, 0f, -rear),
                new Vector3(-halfWidth + radius, 0f, -rear),
                new Vector3(-halfWidth, 0f, -rear + radius),
                new Vector3(-halfWidth, 0f, front - radius),
                new Vector3(-halfWidth + radius, 0f, front),
                new Vector3(halfWidth - radius, 0f, front),
                new Vector3(halfWidth, 0f, front - radius)
            };
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            for (int index = 0; index < RoundedKnotCount; index++)
            {
                Vector3 anchor = anchors[index];
                Vector3 corner = corners[index / 2];
                Vector3 previous = anchors[(index + RoundedKnotCount - 1) % RoundedKnotCount];
                Vector3 next = anchors[(index + 1) % RoundedKnotCount];
                BezierOrbitKnot knot = new BezierOrbitKnot();
                if (index % 2 == 0)
                {
                    knot.Configure(anchor, anchor + (previous - anchor) / 3f, anchor + (corner - anchor) * ArcHandleScale);
                }
                else
                {
                    knot.Configure(anchor, anchor + (corner - anchor) * ArcHandleScale, anchor + (next - anchor) / 3f);
                }

                knots.Add(knot);
            }

            orbit.Configure(knots, Quaternion.identity);
            float edge = 2f * halfWidth - 2f * radius;
            float side = front + rear - 2f * radius;
            float arc = Mathf.PI * radius * 0.5f;
            float perimeter = 2f * (edge + side) + 4f * arc;
            OrbitRemovableSection frontSection = new OrbitRemovableSection();
            frontSection.Configure((2f * arc + edge + side) / perimeter, (4f * arc + 2f * edge + side) / perimeter);
            OrbitRemovableSection rearSection = new OrbitRemovableSection();
            rearSection.Configure(0f, (2f * arc + edge) / perimeter);
            orbit.ConfigureRemovableSections(frontSection, rearSection);
            profile.AddWatchMarker(orbit, "Right", (4f * arc + 2f * edge + side * 1.5f) / perimeter, new Vector3(0f, 1f, 0f), true);
            VehicleConnectorAnchors connectorAnchors = new VehicleConnectorAnchors();
            connectorAnchors.Configure(new Vector3(0f, 0f, front + HitchGap), new Vector3(0f, 0f, -rear - HitchGap));
            profile.ConfigureConnectorAnchors(connectorAnchors);
            return profile;
        }

        public OrbitConnectorPairOverride CreateOverride(VehicleProfile front, VehicleProfile rear, GeneratedOrbitConnectorPair pair, OrbitAttachmentEnd frontEnd, OrbitAttachmentEnd rearEnd)
        {
            OrbitConnectorGeometry left = new OrbitConnectorGeometry();
            OrbitConnectorGeometry right = new OrbitConnectorGeometry();
            GeneratedOrbitConnector leftSource = pair.LeftConnector;
            GeneratedOrbitConnector rightSource = pair.RightConnector;
            left.Configure(leftSource.StartPosition, leftSource.StartControlPoint + Vector3.left * 0.25f, leftSource.EndControlPoint + Vector3.left * 0.25f, leftSource.EndPosition);
            right.Configure(rightSource.StartPosition, rightSource.StartControlPoint + Vector3.right * 0.25f, rightSource.EndControlPoint + Vector3.right * 0.25f, rightSource.EndPosition);
            OrbitConnectorPairOverride result = ScriptableObject.CreateInstance<OrbitConnectorPairOverride>();
            result.Configure(front, rear, frontEnd, rearEnd, left, right);
            objects.Add(result);
            return result;
        }

        public Transform CreateBody(string name)
        {
            GameObject body = new GameObject(name);
            objects.Add(body);
            return body.transform;
        }

        public void Dispose()
        {
            for (int index = objects.Count - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(objects[index]);
            }

            objects.Clear();
        }

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 previous, Vector3 next)
        {
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, anchor + (previous - anchor) / 3f, anchor + (next - anchor) / 3f);
            return knot;
        }
    }
}
