using UnityEngine;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Gley.CameraSystem.Input
{
    public class TouchGestureRecognizer
    {
        private const int MaximumTouches = 16;

        private readonly int[] ids = new int[MaximumTouches];
        private readonly Vector2[] starts = new Vector2[MaximumTouches];
        private readonly Vector2[] positions = new Vector2[MaximumTouches];
        private readonly Vector2[] previousPositions = new Vector2[MaximumTouches];
        private readonly bool[] active = new bool[MaximumTouches];
        private readonly bool[] eligible = new bool[MaximumTouches];
        private readonly bool[] dragging = new bool[MaximumTouches];
        private readonly bool[] suppressed = new bool[MaximumTouches];
        private float width;
        private float height;
        private float time;
        private float dragThreshold = 0.01f;
        private float doubleTapInterval = 0.3f;
        private float doubleTapDistance = 0.02f;
        private float previousTapTime = float.NegativeInfinity;
        private float previousPinchSpan;
        private Vector2 previousTapPosition;
        private bool pinchActive;
        private bool mouseDown;

        public Vector2 Drag { get; private set; }
        public float Pinch { get; private set; }
        public bool DoubleTapped { get; private set; }

        public void Configure(float dragStart, float tapInterval, float tapDistance)
        {
            dragThreshold = Mathf.Max(0f, dragStart);
            doubleTapInterval = Mathf.Max(0f, tapInterval);
            doubleTapDistance = Mathf.Max(0f, tapDistance);
        }

        public void BeginFrame(float screenWidth, float screenHeight, float currentTime)
        {
            width = Mathf.Max(1f, screenWidth);
            height = Mathf.Max(1f, screenHeight);
            time = currentTime;
            Drag = Vector2.zero;
            Pinch = 0f;
            DoubleTapped = false;
            for (int index = 0; index < MaximumTouches; index++)
            {
                if (active[index])
                {
                    previousPositions[index] = positions[index];
                }
            }
        }

        public void FeedTouch(int id, TouchPhase phase, Vector2 position, bool startedOverUi, bool startedInArea)
        {
            if (phase == TouchPhase.Began)
            {
                int index = FindFreeSlot();
                if (index < 0)
                {
                    return;
                }

                ids[index] = id;
                starts[index] = position;
                positions[index] = position;
                previousPositions[index] = position;
                active[index] = true;
                eligible[index] = !startedOverUi && startedInArea;
                dragging[index] = false;
                suppressed[index] = false;
                return;
            }

            int slot = FindSlot(id);
            if (slot < 0)
            {
                return;
            }

            positions[slot] = position;
            if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
            {
                if (phase == TouchPhase.Ended && eligible[slot] && !dragging[slot] && !suppressed[slot] && CountEligible() == 1 &&
                    Vector2.Distance(position, starts[slot]) <= dragThreshold * width)
                {
                    RegisterTap(position);
                }

                active[slot] = false;
            }
        }

        public void FeedMouse(bool pressed, Vector2 position, bool startedOverUi, bool startedInArea)
        {
            if (pressed && !mouseDown)
            {
                FeedTouch(int.MinValue, TouchPhase.Began, position, startedOverUi, startedInArea);
            }
            else if (pressed)
            {
                FeedTouch(int.MinValue, TouchPhase.Moved, position, false, false);
            }
            else if (mouseDown)
            {
                FeedTouch(int.MinValue, TouchPhase.Ended, position, false, false);
            }

            mouseDown = pressed;
        }

        public void FeedMouseScroll(float amount, bool overUi, bool inArea)
        {
            if (!overUi && inArea)
            {
                Pinch += amount;
            }
        }

        public void EndFrame()
        {
            int first = -1;
            int second = -1;
            for (int index = 0; index < MaximumTouches; index++)
            {
                if (!active[index] || !eligible[index])
                {
                    continue;
                }

                if (first < 0)
                {
                    first = index;
                }
                else
                {
                    second = index;
                    break;
                }
            }

            if (second >= 0)
            {
                suppressed[first] = true;
                suppressed[second] = true;
                float span = Vector2.Distance(positions[first], positions[second]);
                if (pinchActive)
                {
                    Pinch += (span - previousPinchSpan) / width;
                }

                previousPinchSpan = span;
                pinchActive = true;
                return;
            }

            pinchActive = false;
            if (first < 0 || suppressed[first])
            {
                return;
            }

            Vector2 travel = positions[first] - starts[first];
            if (!dragging[first])
            {
                if (travel.magnitude <= dragThreshold * width)
                {
                    return;
                }

                dragging[first] = true;
            }

            Vector2 delta = positions[first] - previousPositions[first];
            Drag = new Vector2(delta.x / width, delta.y / height);
        }

        public void Clear()
        {
            for (int index = 0; index < MaximumTouches; index++)
            {
                active[index] = false;
            }

            pinchActive = false;
            mouseDown = false;
            previousTapTime = float.NegativeInfinity;
            Drag = Vector2.zero;
            Pinch = 0f;
            DoubleTapped = false;
        }

        private int FindSlot(int id)
        {
            for (int index = 0; index < MaximumTouches; index++)
            {
                if (active[index] && ids[index] == id)
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindFreeSlot()
        {
            for (int index = 0; index < MaximumTouches; index++)
            {
                if (!active[index])
                {
                    return index;
                }
            }

            return -1;
        }

        private int CountEligible()
        {
            int count = 0;
            for (int index = 0; index < MaximumTouches; index++)
            {
                if (active[index] && eligible[index])
                {
                    count++;
                }
            }

            return count;
        }

        private void RegisterTap(Vector2 position)
        {
            if (time - previousTapTime <= doubleTapInterval && Vector2.Distance(position, previousTapPosition) <= doubleTapDistance * width)
            {
                DoubleTapped = true;
                previousTapTime = float.NegativeInfinity;
            }
            else
            {
                previousTapPosition = position;
                previousTapTime = time;
            }
        }
    }
}
