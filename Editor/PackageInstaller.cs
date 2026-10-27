using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Events;

namespace Gley.CameraSystem.Editor
{
    public class PackageInstaller
    {
        private UnityAction<string> statusChanged;
        private AddRequest request;
        private string packageName;
        private bool isInstalling;

        public void Install(string packageToInstall, UnityAction<string> onStatusChanged)
        {
            if (isInstalling)
            {
                return;
            }

            isInstalling = true;
            packageName = packageToInstall;
            statusChanged = onStatusChanged;
            EditorApplication.delayCall += StartInstall;
        }

        private void StartInstall()
        {
            EditorApplication.delayCall -= StartInstall;
            Debug.Log("Installing: " + packageName + ". Please wait...");
            request = Client.Add(packageName);
            EditorApplication.update += UpdateInstallProgress;
        }

        private void UpdateInstallProgress()
        {
            statusChanged(request.Status.ToString());

            if (!request.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= UpdateInstallProgress;

            if (request.Status == StatusCode.Success)
            {
                statusChanged("Installed: " + request.Result.packageId);
                Debug.Log("Installed: " + request.Result.packageId);
            }
            else
            {
                Debug.LogError(request.Error.message);
                statusChanged(request.Error.message);
            }

            request = null;
            isInstalling = false;
        }
    }
}
