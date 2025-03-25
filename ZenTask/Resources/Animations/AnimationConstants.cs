using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenTask.Resources.Animations
{
    public static class AnimationConstants
    {
        // Duration constants
        public const double DURATION_VERY_SHORT = 0.1;  // 100ms - Button clicks, micro-feedback
        public const double DURATION_SHORT = 0.2;       // 200ms - Simple transitions
        public const double DURATION_MEDIUM = 0.3;      // 300ms - Standard transitions
        public const double DURATION_LONG = 0.5;        // 500ms - Complex transitions
        public const double DURATION_VERY_LONG = 0.8;   // 800ms - Elaborate animations

        // Easing parameters
        public const double BACK_AMPLITUDE = 0.3;       // Amplitude for back ease (bouncy effect)
        public const double ELASTIC_OSCILLATIONS = 1;   // Oscillations for elastic ease
        public const double ELASTIC_SPRINGINESS = 3;    // Springiness for elastic ease
        public const int POWER_EASE_POWER = 3;          // Power for power ease functions

        // Animation types
        public const string TYPE_FADE = "Fade";
        public const string TYPE_SCALE = "Scale";
        public const string TYPE_SLIDE = "Slide";
        public const string TYPE_TRANSFORM = "Transform";
    }
}
