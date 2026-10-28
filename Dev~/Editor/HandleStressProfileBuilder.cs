using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor.Dev
{
    public class HandleStressProfileBuilder
    {
        private const string MenuPath = "Tools/GleyDev/CameraSystem/Create Handle Stress Profile";
        private const string DataFolder = "Assets/VehicleCameraSystemData";
        private const string ProfileFolder = "Assets/VehicleCameraSystemData/Profiles";
        private const string ProfilePath = "Assets/VehicleCameraSystemData/Profiles/Handle Stress Profile.asset";
        private const float HalfWidth = 2f;
        private const float BodyLength = 12f;
        private const float CornerRadius = 1.5f;
        private const float ArcHandleScale = 0.5523f;
        private const int BodyCount = 5;
        private const int MarkersPerBody = 20;
        private const int KnotCount = 8;

        [MenuItem(MenuPath)]
        private static void CreateProfile()
        {
            new HandleStressProfileBuilder().Build();
        }

        public void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<VehicleProfile>(ProfilePath) != null && !EditorUtility.DisplayDialog("Handle Stress Profile", $"{ProfilePath} already exists. Replace it?", "Replace", "Cancel"))
            {
                return;
            }

            CreateFolders();
            AssetDatabase.DeleteAsset(ProfilePath);
            VehicleProfile profile = ScriptableObject.CreateInstance<VehicleProfile>();
            profile.EnsureProfileId();
            VehicleOrbit orbit = profile.AddOrbit("Presentation");
            float halfLength = BodyLength * BodyCount * 0.5f;
            orbit.Configure(CreateKnots(halfLength), Quaternion.identity);
            orbit.ConfigureBaseHeight(1.5f);
            orbit.ConfigureOffsetRanges(-1f, 4f, -4f, 1f);
            AddMarkers(profile, orbit, halfLength);
            AssetDatabase.CreateAsset(profile, ProfilePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
        }

        private void CreateFolders()
        {
            if (!AssetDatabase.IsValidFolder(DataFolder))
            {
                AssetDatabase.CreateFolder("Assets", "VehicleCameraSystemData");
            }

            if (!AssetDatabase.IsValidFolder(ProfileFolder))
            {
                AssetDatabase.CreateFolder(DataFolder, "Profiles");
            }
        }

        private List<BezierOrbitKnot> CreateKnots(float halfLength)
        {
            Vector3[] corners =
            {
                new Vector3(HalfWidth, 0f, -halfLength),
                new Vector3(-HalfWidth, 0f, -halfLength),
                new Vector3(-HalfWidth, 0f, halfLength),
                new Vector3(HalfWidth, 0f, halfLength)
            };
            Vector3[] anchors =
            {
                new Vector3(HalfWidth, 0f, -halfLength + CornerRadius),
                new Vector3(HalfWidth - CornerRadius, 0f, -halfLength),
                new Vector3(-HalfWidth + CornerRadius, 0f, -halfLength),
                new Vector3(-HalfWidth, 0f, -halfLength + CornerRadius),
                new Vector3(-HalfWidth, 0f, halfLength - CornerRadius),
                new Vector3(-HalfWidth + CornerRadius, 0f, halfLength),
                new Vector3(HalfWidth - CornerRadius, 0f, halfLength),
                new Vector3(HalfWidth, 0f, halfLength - CornerRadius)
            };
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();

            for (int index = 0; index < KnotCount; index++)
            {
                Vector3 anchor = anchors[index];
                Vector3 corner = corners[index / 2];
                Vector3 previous = anchors[(index + KnotCount - 1) % KnotCount];
                Vector3 next = anchors[(index + 1) % KnotCount];
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

            return knots;
        }

        private void AddMarkers(VehicleProfile profile, VehicleOrbit orbit, float halfLength)
        {
            int markerCount = BodyCount * MarkersPerBody;

            for (int markerIndex = 0; markerIndex < markerCount; markerIndex++)
            {
                int bodyIndex = markerIndex / MarkersPerBody;
                float normalizedPosition = (markerIndex + 0.5f) / markerCount;
                float watchZ = -halfLength + BodyLength * (bodyIndex + 0.5f);
                Vector3 watchPoint = new Vector3(0f, 1.5f, watchZ);
                profile.AddWatchMarker(orbit, $"Body {bodyIndex + 1} Point {markerIndex % MarkersPerBody + 1}", normalizedPosition, watchPoint, true);
            }
        }
    }
}
