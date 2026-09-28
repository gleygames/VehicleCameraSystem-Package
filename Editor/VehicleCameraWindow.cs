using System.Collections.Generic;
using Gley.Common.Editor;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraWindow : EditorWindow
    {
        private const int MainTabCount = 3;
        private const int ConnectionsTabIndex = 3;
        private const float NewProfileButtonWidth = 100f;
        private const float SceneHandlesButtonWidth = 110f;

        private readonly List<List<EditorIssue>> tabIssues = new List<List<EditorIssue>>();
        private readonly GUIContent[] mainTabContents = new GUIContent[MainTabCount];
        private readonly GUIContent[] advancedTabContents = new GUIContent[1];
        private readonly List<IVehicleCameraTab> tabs = new List<IVehicleCameraTab>();

        [SerializeField] private VehicleProfile profile;
        [SerializeField] private ProfileGenerationSettings generationSettings = new ProfileGenerationSettings();
        [SerializeField] private Vector2 scrollPosition;
        private VehicleCameraWindowContext context;
        private VehicleCameraValidationPanel validationPanel;
        private ProfileAssetSaver profileSaver;
        private SerializedObject serializedWindow;
        [SerializeField] private int selectedTabIndex;
        private int seenSelectionVersion;
        [SerializeField] private bool isAdvancedExpanded;

        [MenuItem(VehicleCameraWindowProperties.MenuItem, false, 10)]
        private static void OpenWindow()
        {
            WindowLoader.LoadWindow<VehicleCameraWindow>(new VehicleCameraWindowProperties(), new VehicleCameraSystemVersion(), out string _);
        }

        private void OnEnable()
        {
            VehicleCameraWindowProperties properties = new VehicleCameraWindowProperties();
            titleContent = new GUIContent(properties.WindowName + new VehicleCameraSystemVersion().LongVersion);

            IInputSystemInstallation inputSystemInstallation = new InputSystemInstallation();
            context = new VehicleCameraWindowContext(new VehicleCameraValidationCollector(inputSystemInstallation));
            context.SetProfile(profile);
            validationPanel = new VehicleCameraValidationPanel(context);
            profileSaver = new ProfileAssetSaver(new VehicleCameraAssetSaver(properties));
            if (generationSettings == null)
            {
                generationSettings = new ProfileGenerationSettings();
            }

            serializedWindow = new SerializedObject(this);

            tabs.Clear();
            tabs.Add(new VehicleCameraSetupTab(context, inputSystemInstallation, profileSaver, generationSettings, serializedWindow, serializedWindow.FindProperty(nameof(generationSettings))));
            tabs.Add(new VehicleCameraOrbitsTab(context));
            tabs.Add(new VehicleCameraViewsTab(context));
            tabs.Add(new VehicleCameraConnectionsTab(context));

            tabIssues.Clear();
            for (int index = 0; index < tabs.Count; index++)
            {
                tabIssues.Add(new List<EditorIssue>());
            }

            if (selectedTabIndex < 0 || selectedTabIndex >= tabs.Count)
            {
                selectedTabIndex = 0;
            }

            UpdateTabContents();

            Undo.undoRedoPerformed += HandleUndoRedo;
            SceneView.duringSceneGui += HandleSceneGUI;
            ObjectChangeEvents.changesPublished += HandleChangesPublished;
        }

        private void UpdateTabContents()
        {
            for (int index = 0; index < MainTabCount; index++)
            {
                mainTabContents[index] = CreateTabContent(index);
            }

            advancedTabContents[0] = CreateTabContent(ConnectionsTabIndex);
        }

        private GUIContent CreateTabContent(int tabIndex)
        {
            List<EditorIssue> issues = tabIssues[tabIndex];
            string title = tabs[tabIndex].Title;
            int errorCount = 0;

            for (int index = 0; index < issues.Count; index++)
            {
                if (issues[index].Severity == EditorIssueSeverity.Error)
                {
                    errorCount++;
                }
            }

            if (errorCount > 0)
            {
                return new GUIContent($"{title} ({issues.Count})", EditorGUIUtility.IconContent("console.erroricon.sml").image);
            }

            if (issues.Count > 0)
            {
                return new GUIContent($"{title} ({issues.Count})", EditorGUIUtility.IconContent("console.warnicon.sml").image);
            }

            return new GUIContent(title);
        }

        private void HandleUndoRedo()
        {
            context.HandleUndoRedo();
            Repaint();
            SceneView.RepaintAll();
        }

        private void HandleSceneGUI(SceneView sceneView)
        {
            if (!context.SceneHandles.IsDrawingEnabled)
            {
                return;
            }

            if (selectedTabIndex >= 0 && selectedTabIndex < tabs.Count)
            {
                tabs[selectedTabIndex].OnSceneGUI(sceneView);
            }

            if (seenSelectionVersion != context.SceneHandles.SelectionVersion)
            {
                seenSelectionVersion = context.SceneHandles.SelectionVersion;
                Repaint();
            }
        }

        private void HandleChangesPublished(ref ObjectChangeEventStream stream)
        {
            context.MarkIssuesDirty();
            Repaint();
        }

        private void OnSelectionChange()
        {
            VehicleProfile selectedProfile = Selection.activeObject as VehicleProfile;
            if (selectedProfile != null && selectedProfile != profile)
            {
                SetProfile(selectedProfile);
                Repaint();
            }
        }

        private void SetProfile(VehicleProfile newProfile)
        {
            profile = newProfile;
            context.SetProfile(newProfile);
        }

        private void OnFocus()
        {
            if (context != null)
            {
                context.MarkIssuesDirty();
            }
        }

        private void OnProjectChange()
        {
            if (context != null)
            {
                context.MarkIssuesDirty();
                Repaint();
            }
        }

        private void OnInspectorUpdate()
        {
            if (context != null && context.AreIssuesDirty)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (context == null)
            {
                return;
            }

            if (context.Profile != profile)
            {
                profile = context.Profile;
            }

            if (Event.current.type == EventType.Layout && context.AreIssuesDirty)
            {
                CollectIssues();
            }

            DrawHeader();
            DrawTabBar();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            validationPanel.DrawIssues(tabIssues[selectedTabIndex]);
            EditorGUILayout.Space();
            tabs[selectedTabIndex].DrawTab();
            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }

        private void CollectIssues()
        {
            for (int index = 0; index < tabs.Count; index++)
            {
                tabIssues[index].Clear();
                tabs[index].CollectIssues(tabIssues[index]);
            }

            UpdateTabContents();
            context.ClearIssuesDirty();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            VehicleProfile selectedProfile = (VehicleProfile)EditorGUILayout.ObjectField("Vehicle Profile", profile, typeof(VehicleProfile), false);
            if (selectedProfile != profile)
            {
                SetProfile(selectedProfile);
            }

            if (GUILayout.Button("New Profile", GUILayout.Width(NewProfileButtonWidth)))
            {
                EditorApplication.delayCall += CreateNewProfile;
            }

            bool isDrawingEnabled = GUILayout.Toggle(context.SceneHandles.IsDrawingEnabled, "Scene Handles", "Button", GUILayout.Width(SceneHandlesButtonWidth));
            context.SceneHandles.SetDrawingEnabled(isDrawingEnabled);

            EditorGUILayout.EndHorizontal();
        }

        private void CreateNewProfile()
        {
            EditorApplication.delayCall -= CreateNewProfile;
            VehicleProfile newProfile = CreateInstance<VehicleProfile>();
            if (profileSaver.SaveNewProfile(newProfile, "Vehicle Profile") == null)
            {
                DestroyImmediate(newProfile);
                return;
            }

            if (context != null)
            {
                SetProfile(newProfile);
                Repaint();
            }

            EditorGUIUtility.PingObject(newProfile);
        }

        private void DrawTabBar()
        {
            int mainSelection = -1;
            if (selectedTabIndex < MainTabCount)
            {
                mainSelection = selectedTabIndex;
            }

            int newMainSelection = GUILayout.Toolbar(mainSelection, mainTabContents);
            if (newMainSelection != mainSelection && newMainSelection >= 0)
            {
                selectedTabIndex = newMainSelection;
            }

            isAdvancedExpanded = EditorGUILayout.Foldout(isAdvancedExpanded, "Advanced", true);
            if (!isAdvancedExpanded)
            {
                if (selectedTabIndex == ConnectionsTabIndex)
                {
                    selectedTabIndex = 0;
                }

                return;
            }

            int advancedSelection = -1;
            if (selectedTabIndex == ConnectionsTabIndex)
            {
                advancedSelection = 0;
            }

            int newAdvancedSelection = GUILayout.Toolbar(advancedSelection, advancedTabContents);
            if (newAdvancedSelection == 0 && advancedSelection != 0)
            {
                selectedTabIndex = ConnectionsTabIndex;
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
            SceneView.duringSceneGui -= HandleSceneGUI;
            ObjectChangeEvents.changesPublished -= HandleChangesPublished;
            EditorApplication.delayCall -= CreateNewProfile;
            if (serializedWindow != null)
            {
                serializedWindow.Dispose();
                serializedWindow = null;
            }
        }
    }
}
