namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// kikito tween.lua stepped runner for YourFlyouts TweenNode / TweenNode1 channels.
    /// </summary>
    public sealed class TesseraFlyoutStepTween
    {
        private readonly int _duration;
        private readonly double _from;
        private readonly double _to;
        private readonly Func<double, double, double, double, double> _ease;
        private int _clock;

        public TesseraFlyoutStepTween(int durationSteps, double from, double to, string? easeType)
        {
            _duration = Math.Max(1, durationSteps);
            _from = from;
            _to = to;
            _ease = TesseraFlyoutTweenEngine.ResolveEasingFunction(easeType);
            _clock = 0;
            Value = _ease(_clock, _from, _to - _from, _duration);
        }

        public double Value { get; private set; }

        public bool IsCompleteForward => _clock >= _duration;

        public bool IsCompleteBackward => _clock <= 0;

        /// <summary>Matches tween.lua <c>t:update(±1)</c>.</summary>
        public bool Advance(int delta)
        {
            _clock = Math.Clamp(_clock + delta, 0, _duration);
            double change = _to - _from;
            Value = _ease(_clock, _from, change, _duration);
            return delta > 0 ? _clock >= _duration : _clock <= 0;
        }

        public void Reset(double value)
        {
            _clock = 0;
            Value = value;
        }
    }

    /// <summary>Penner easing ported from YourFlyouts <c>@Resources/Lua/tween.lua</c>.</summary>
    public static class TesseraFlyoutTweenEngine
    {
        private const double Pi = Math.PI;

        public static Func<double, double, double, double, double> ResolveEasingFunction(string? easeType)
        {
            string name = TesseraFlyoutAnimationPolicy.NormalizeEase(easeType).ToLowerInvariant();
            return name switch
            {
                "linear" => Linear,
                "insine" => InSine,
                "outsine" => OutSine,
                "inoutsine" => InOutSine,
                "inquad" => InQuad,
                "outquad" => OutQuad,
                "inoutquad" => InOutQuad,
                "incubic" => InCubic,
                "outcubic" => OutCubic,
                "inoutcubic" => InOutCubic,
                "inquart" => InQuart,
                "outquart" => OutQuart,
                "inoutquart" => InOutQuart,
                "inquint" => InQuint,
                "outquint" => OutQuint,
                "inoutquint" => InOutQuint,
                "inexpo" => InExpo,
                "outexpo" => OutExpo,
                "inoutexpo" => InOutExpo,
                "incirc" => InCirc,
                "outcirc" => OutCirc,
                "inoutcirc" => InOutCirc,
                "inback" => (t, b, c, d) => InBack(t, b, c, d),
                "outback" => (t, b, c, d) => OutBack(t, b, c, d),
                "inoutback" => (t, b, c, d) => InOutBack(t, b, c, d),
                "inbounce" => InBounce,
                "outbounce" => OutBounce,
                "inoutbounce" => InOutBounce,
                "inelastic" => InElastic,
                "outelastic" => OutElastic,
                "inoutelastic" => InOutElastic,
                _ => OutQuart,
            };
        }

        public static double SampleNormalized(int clock, int durationSteps, string? easeType, bool entrance)
        {
            TesseraFlyoutStepTween tween = new(durationSteps, 0, 100, easeType);
            int delta = entrance ? 1 : -1;
            for (int i = 0; i < Math.Abs(clock); i++)
            {
                _ = tween.Advance(delta);
            }

            return Math.Clamp(tween.Value / 100.0, 0, 1);
        }

        private static double Linear(double t, double b, double c, double d)
        {
            return (c * t / d) + b;
        }

        private static double InQuad(double t, double b, double c, double d)
        {
            return (c * Pow(t / d, 2)) + b;
        }

        private static double OutQuad(double t, double b, double c, double d)
        {
            t /= d;
            return (-c * t * (t - 2)) + b;
        }

        private static double InOutQuad(double t, double b, double c, double d)
        {
            t = t / d * 2;
            return t < 1 ? (c / 2 * Pow(t, 2)) + b : (-c / 2 * (((t - 1) * (t - 3)) - 1)) + b;
        }

        private static double InCubic(double t, double b, double c, double d)
        {
            return (c * Pow(t / d, 3)) + b;
        }

        private static double OutCubic(double t, double b, double c, double d)
        {
            return (c * (Pow((t / d) - 1, 3) + 1)) + b;
        }

        private static double InOutCubic(double t, double b, double c, double d)
        {
            t = t / d * 2;
            if (t < 1)
            {
                return (c / 2 * t * t * t) + b;
            }

            t -= 2;
            return (c / 2 * ((t * t * t) + 2)) + b;
        }

        private static double InQuart(double t, double b, double c, double d)
        {
            return (c * Pow(t / d, 4)) + b;
        }

        private static double OutQuart(double t, double b, double c, double d)
        {
            return (-c * (Pow((t / d) - 1, 4) - 1)) + b;
        }

        private static double InOutQuart(double t, double b, double c, double d)
        {
            t = t / d * 2;
            return t < 1 ? (c / 2 * Pow(t, 4)) + b : (-c / 2 * (Pow(t - 2, 4) - 2)) + b;
        }

        private static double InQuint(double t, double b, double c, double d)
        {
            return (c * Pow(t / d, 5)) + b;
        }

        private static double OutQuint(double t, double b, double c, double d)
        {
            return (c * (Pow((t / d) - 1, 5) + 1)) + b;
        }

        private static double InOutQuint(double t, double b, double c, double d)
        {
            t = t / d * 2;
            return t < 1 ? (c / 2 * Pow(t, 5)) + b : (c / 2 * (Pow(t - 2, 5) + 2)) + b;
        }

        private static double InSine(double t, double b, double c, double d)
        {
            return (-c * Cos(t / d * (Pi / 2))) + c + b;
        }

        private static double OutSine(double t, double b, double c, double d)
        {
            return (c * Sin(t / d * (Pi / 2))) + b;
        }

        private static double InOutSine(double t, double b, double c, double d)
        {
            return (-c / 2 * (Cos(Pi * t / d) - 1)) + b;
        }

        private static double InExpo(double t, double b, double c, double d)
        {
            return t == 0 ? b : (c * Pow(2, 10 * ((t / d) - 1))) + b - (c * 0.001);
        }

        private static double OutExpo(double t, double b, double c, double d)
        {
            return t == d ? b + c : (c * 1.001 * (-Pow(2, -10 * t / d) + 1)) + b;
        }

        private static double InOutExpo(double t, double b, double c, double d)
        {
            if (t == 0)
            {
                return b;
            }

            if (t == d)
            {
                return b + c;
            }

            t = t / d * 2;
            return t < 1 ? (c / 2 * Pow(2, 10 * (t - 1))) + b - (c * 0.0005) : (c / 2 * 1.0005 * (-Pow(2, -10 * (t - 1)) + 2)) + b;
        }

        private static double InCirc(double t, double b, double c, double d)
        {
            return (-c * (Sqrt(1 - Pow(t / d, 2)) - 1)) + b;
        }

        private static double OutCirc(double t, double b, double c, double d)
        {
            return (c * Sqrt(1 - Pow((t / d) - 1, 2))) + b;
        }

        private static double InOutCirc(double t, double b, double c, double d)
        {
            t = t / d * 2;
            if (t < 1)
            {
                return (-c / 2 * (Sqrt(1 - (t * t)) - 1)) + b;
            }

            t -= 2;
            return (c / 2 * (Sqrt(1 - (t * t)) + 1)) + b;
        }

        private static double InBack(double t, double b, double c, double d, double s = 1.70158)
        {
            t /= d;
            return (c * t * t * (((s + 1) * t) - s)) + b;
        }

        private static double OutBack(double t, double b, double c, double d, double s = 1.70158)
        {
            t = (t / d) - 1;
            return (c * ((t * t * (((s + 1) * t) + s)) + 1)) + b;
        }

        private static double InOutBack(double t, double b, double c, double d, double s = 1.70158)
        {
            s *= 1.525;
            t = t / d * 2;
            if (t < 1)
            {
                return (c / 2 * (t * t * (((s + 1) * t) - s))) + b;
            }

            t -= 2;
            return (c / 2 * ((t * t * (((s + 1) * t) + s)) + 2)) + b;
        }

        private static double OutBounce(double t, double b, double c, double d)
        {
            t /= d;
            if (t < 1 / 2.75)
            {
                return (c * (7.5625 * t * t)) + b;
            }

            if (t < 2 / 2.75)
            {
                t -= 1.5 / 2.75;
                return (c * ((7.5625 * t * t) + 0.75)) + b;
            }
            if (t < 2.5 / 2.75)
            {
                t -= 2.25 / 2.75;
                return (c * ((7.5625 * t * t) + 0.9375)) + b;
            }
            t -= 2.625 / 2.75;
            return (c * ((7.5625 * t * t) + 0.984375)) + b;
        }

        private static double InBounce(double t, double b, double c, double d)
        {
            return c - OutBounce(d - t, 0, c, d) + b;
        }

        private static double InOutBounce(double t, double b, double c, double d)
        {
            return t < d / 2 ? (InBounce(t * 2, 0, c, d) * 0.5) + b : (OutBounce((t * 2) - d, 0, c, d) * 0.5) + (c * 0.5) + b;
        }

        private static double InElastic(double t, double b, double c, double d)
        {
            if (t == 0)
            {
                return b;
            }

            t /= d;
            if (t == 1)
            {
                return b + c;
            }

            (double p, double a, double s) = CalculatePas(d, c);
            t -= 1;
            return -(a * Pow(2, 10 * t) * Sin(((t * d) - s) * (2 * Pi) / p)) + b;
        }

        private static double OutElastic(double t, double b, double c, double d)
        {
            if (t == 0)
            {
                return b;
            }

            t /= d;
            if (t == 1)
            {
                return b + c;
            }

            (double p, double a, double s) = CalculatePas(d, c);
            return (a * Pow(2, -10 * t) * Sin(((t * d) - s) * (2 * Pi) / p)) + c + b;
        }

        private static double InOutElastic(double t, double b, double c, double d)
        {
            if (t == 0)
            {
                return b;
            }

            t = t / d * 2;
            if (t == 2)
            {
                return b + c;
            }

            (double p, double a, double s) = CalculatePas(d, c);
            t -= 1;
            return t < 0
                ? (-0.5 * (a * Pow(2, 10 * t) * Sin(((t * d) - s) * (2 * Pi) / p))) + b
                : (a * Pow(2, -10 * t) * Sin(((t * d) - s) * (2 * Pi) / p) * 0.5) + c + b;
        }

        private static (double P, double A, double S) CalculatePas(double d, double c, double p = 0, double a = 0)
        {
            p = p == 0 ? d * 0.3 : p;
            a = a == 0 ? c : a;
            if (a < Abs(c))
            {
                return (p, c, p / 4);
            }

            return (p, a, p / (2 * Pi) * Asin(c / a));
        }

        private static double Pow(double x, double y)
        {
            return Math.Pow(x, y);
        }

        private static double Sin(double x)
        {
            return Math.Sin(x);
        }

        private static double Cos(double x)
        {
            return Math.Cos(x);
        }

        private static double Sqrt(double x)
        {
            return Math.Sqrt(x);
        }

        private static double Abs(double x)
        {
            return Math.Abs(x);
        }

        private static double Asin(double x)
        {
            return Math.Asin(x);
        }
    }
}
