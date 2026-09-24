namespace Gley.CameraSystem.Editor
{
    public class CommonVersionComparer
    {
        public bool IsSatisfied(int required, int found)
        {
            return found >= required;
        }
    }
}
