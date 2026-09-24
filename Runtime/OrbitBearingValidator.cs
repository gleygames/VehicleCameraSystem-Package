using System.Collections.Generic;

namespace Gley.CameraSystem
{
    public class OrbitBearingValidator
    {
        private readonly List<float> ambiguousBearings = new List<float>(360);

        public IReadOnlyList<float> FindAmbiguousBearings(OrbitBearing bearing)
        {
            ambiguousBearings.Clear();

            if (bearing == null)
            {
                return ambiguousBearings;
            }

            for (int degrees = -179; degrees <= 180; degrees++)
            {
                if (bearing.CountCrossings(degrees) > 1)
                {
                    ambiguousBearings.Add(degrees);
                }
            }

            return ambiguousBearings;
        }
    }
}
