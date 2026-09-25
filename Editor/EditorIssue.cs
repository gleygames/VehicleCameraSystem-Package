using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public readonly struct EditorIssue
    {
        public EditorIssueSeverity Severity { get; }
        public Object Target { get; }
        public string Message { get; }
        public int ItemId { get; }

        public EditorIssue(EditorIssueSeverity severity, string message, Object target, int itemId)
        {
            Severity = severity;
            Message = message;
            Target = target;
            ItemId = itemId;
        }
    }
}
