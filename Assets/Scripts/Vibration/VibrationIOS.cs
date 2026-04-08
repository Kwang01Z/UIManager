#if UNITY_IOS
using System;
using System.Runtime.InteropServices;
using static VibrationUtility.VibrationUtil;

namespace VibrationUtility.Instance
{
    public class VibrationIOS : VibrationInstance
    {
		[DllImport("__Internal")]
		public static extern void Vibrate(int _n);
		[DllImport("__Internal")]
		public static extern void _impactOccurred(string style);
        [DllImport("__Internal")]
        public static extern void _impactOccurredWithIntensity(string style, float intensity);
        [DllImport("__Internal")]
		public static extern void _notificationOccurred(string style);
		
        public override void Vibrate(VibrationType vibrationType, float intensity)
        {
	        try
	        {
		        switch (vibrationType)
		        {
			        case VibrationType.Default:
			        case VibrationType.Peek:
			        case VibrationType.Pop:
			        case VibrationType.Nope:
				        Vibrate((int)vibrationType);
				        break;

			        case VibrationType.Heavy:
			        case VibrationType.Medium:
			        case VibrationType.Light:
			        case VibrationType.Rigid:
			        case VibrationType.Soft:
				        _impactOccurredWithIntensity(vibrationType.ToString(), intensity);
				        break;
			        default:
				        _notificationOccurred(vibrationType.ToString());
				        break;
		        }
	        }
	        catch (Exception e)
	        {
	        }
        }

        public override void VibrateCustom(long[] pattern, int[] amplitude)
        {
            
        }

		public override void VibrateFor(long duration, int amplitude)
        {
	        try
	        {
		        _impactOccurredWithIntensity("Soft", duration);
	        }
	        catch (Exception e)
	        {
	        }
        }

        public override bool IsVibrationAvailable()
        {
            return true;
		}
	}
}
#endif
