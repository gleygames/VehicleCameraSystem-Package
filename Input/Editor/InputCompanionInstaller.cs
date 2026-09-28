using Gley.CameraSystem.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Gley.CameraSystem.Input.Editor
{
    public class InputCompanionInstaller : IInputCompanionInstaller
    {
        private const string ButtonsPrefabPath = "Assets/Gley/VehicleCameraSystem/Input/Prefabs/CameraButtons.prefab";

        public void InstallInputCompanion(GameObject cameraObject, CameraSystemController controller)
        {
            VehicleCameraInput existingCompanion = cameraObject.GetComponent<VehicleCameraInput>();
            if (existingCompanion != null)
            {
                return;
            }

            VehicleCameraInput companion = Undo.AddComponent<VehicleCameraInput>(cameraObject);
            EditorUtility.SetDirty(companion);
        }

        public void InstallTouchButtons(GameObject cameraObject, Canvas canvas)
        {
            if (canvas == null || cameraObject == null)
            {
                return;
            }

            VehicleCameraInput companion = cameraObject.GetComponent<VehicleCameraInput>();
            if (companion == null)
            {
                return;
            }

            CameraCommandButton[] existingButtons = canvas.GetComponentsInChildren<CameraCommandButton>(true);
            for (int index = 0; index < existingButtons.Length; index++)
            {
                if (existingButtons[index].Companion == companion)
                {
                    return;
                }
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonsPrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Add Touch Buttons", "CameraButtons.prefab is missing from the input companion package.", "OK");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, canvas.transform) as GameObject;
            if (instance == null)
            {
                return;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Add Vehicle Camera Touch Buttons");
            CameraCommandButton[] buttons = instance.GetComponentsInChildren<CameraCommandButton>(true);
            for (int index = 0; index < buttons.Length; index++)
            {
                Undo.RecordObject(buttons[index], "Link Vehicle Camera Touch Button");
                buttons[index].Configure(companion, buttons[index].Action);
                EditorUtility.SetDirty(buttons[index]);
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
            }

            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    bool addModule = EditorUtility.DisplayDialog("Input System UI Module Required", "The EventSystem has no Input System UI Input Module. Add one?", "Add Module", "Later");
                    if (addModule)
                    {
                        Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
                    }
                }

                return;
            }

            bool addEventSystem = EditorUtility.DisplayDialog("EventSystem Required", "Touch buttons need an EventSystem. Add one with the Input System UI Input Module?", "Add EventSystem", "Later");
            if (!addEventSystem)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystemObject, "Add EventSystem");
            Undo.AddComponent<EventSystem>(eventSystemObject);
            Undo.AddComponent<InputSystemUIInputModule>(eventSystemObject);
        }
    }
}
