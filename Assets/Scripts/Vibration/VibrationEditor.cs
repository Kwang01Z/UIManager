using static VibrationUtility.VibrationUtil;

namespace VibrationUtility.Instance
{
    public class VibrationEditor : VibrationInstance
    {
        public override bool IsVibrationAvailable()
        {
            return false;
        }

        public override void Vibrate(VibrationType _, float __)
        {
            Debug.Log(("Vibrating"));
        }

        public override void VibrateCustom(long[] _, int[] __)
        {
            Debug.Log(("Vibrating"));
        }

        public override void VibrateFor(long duration, int amplitude)
        {
            Debug.Log(("Vibrating for " + duration + "ms with amplitude " + amplitude).ToString());
        }
    }
}