using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Broccoli.Utils
{
	/// <summary>
	/// Easing functions.
	/// </summary>
	public class Easing {
		/// <summary>
		/// Easing modes available.
		/// </summary>
		public enum Mode {
			Linear,
			Clerp,
			Spring,
			EaseInQuad,
			EaseOutQuad,
			EaseInOutQuad,
			EaseInCubic,
			EaseOutCubic,
			EaseInOutCubic,
			EaseInQuart,
			EaseOutQuart,
			EaseInOutQuart,
			EaseInQuint,
			EaseOutQuint,
			EaseInOutQuint,
			EaseInSine,
			EaseOutSine,
			EaseInOutSine,
			EaseInExpo,
			EaseOutExpo,
			EaseOutExpoOver,
			EaseInOutExpo,
			EaseInCirc,
			EaseOutCirc,
			EaseInOutCirc,
			EaseInBounce,
			EaseOutBounce,
			EaseInOutBounce,
			EaseInBack,
			EaseOutBack,
			EaseInOutBack,
			Punch,
			EaseInElastic,
			EaseOutElastic,
			EaseInOutElastic,
		}

		/// <summary>
        /// Applies easing to a value based on the provided mode.
        /// </summary>
        /// <param name="mode">Easing mode to use.</param>
        /// <param name="start">Start value.</param>
        /// <param name="end">End value.</param>
        /// <param name="value">Interpolation value (between 0 and 1).</param>
        /// <returns>Eased value.</returns>
        public static float Ease(Mode mode, float start, float end, float value)
        {
            switch (mode)
            {
                case Mode.Linear: return Linear(start, end, value);
                case Mode.Clerp: return Clerp(start, end, value);
                case Mode.Spring: return Spring(start, end, value);
                case Mode.EaseInQuad: return EaseInQuad(start, end, value);
                case Mode.EaseOutQuad: return EaseOutQuad(start, end, value);
                case Mode.EaseInOutQuad: return EaseInOutQuad(start, end, value);
                case Mode.EaseInCubic: return EaseInCubic(start, end, value);
                case Mode.EaseOutCubic: return EaseOutCubic(start, end, value);
                case Mode.EaseInOutCubic: return EaseInOutCubic(start, end, value);
                case Mode.EaseInQuart: return EaseInQuart(start, end, value);
                case Mode.EaseOutQuart: return EaseOutQuart(start, end, value);
                case Mode.EaseInOutQuart: return EaseInOutQuart(start, end, value);
                case Mode.EaseInQuint: return EaseInQuint(start, end, value);
                case Mode.EaseOutQuint: return EaseOutQuint(start, end, value);
                case Mode.EaseInOutQuint: return EaseInOutQuint(start, end, value);
                case Mode.EaseInSine: return EaseInSine(start, end, value);
                case Mode.EaseOutSine: return EaseOutSine(start, end, value);
                case Mode.EaseInOutSine: return EaseInOutSine(start, end, value);
                case Mode.EaseInExpo: return EaseInExpo(start, end, value);
                case Mode.EaseOutExpo: return EaseOutExpo(start, end, value);
                case Mode.EaseOutExpoOver: return EaseOutExpoOver(start, end, value);
                case Mode.EaseInOutExpo: return EaseInOutExpo(start, end, value);
                case Mode.EaseInCirc: return EaseInCirc(start, end, value);
                case Mode.EaseOutCirc: return EaseOutCirc(start, end, value);
                case Mode.EaseInOutCirc: return EaseInOutCirc(start, end, value);
                case Mode.EaseInBounce: return EaseInBounce(start, end, value);
                case Mode.EaseOutBounce: return EaseOutBounce(start, end, value);
                case Mode.EaseInOutBounce: return EaseInOutBounce(start, end, value);
                case Mode.EaseInBack: return EaseInBack(start, end, value);
                case Mode.EaseOutBack: return EaseOutBack(start, end, value);
                case Mode.EaseInOutBack: return EaseInOutBack(start, end, value);
                case Mode.Punch: return Punch(start, value); // Assuming 'start' is used as amplitude here
                case Mode.EaseInElastic: return EaseInElastic(start, end, value);
                case Mode.EaseOutElastic: return EaseOutElastic(start, end, value);
                case Mode.EaseInOutElastic: return EaseInOutElastic(start, end, value);
                default: return Linear(start, end, value); // Default to linear interpolation
            }
        }

		/// <summary>
        /// Applies easing to a value based on the provided mode.
        /// </summary>
        /// <param name="mode">Easing mode to use.</param>
        /// <param name="start">Start value.</param>
        /// <param name="end">End value.</param>
        /// <param name="value">Interpolation value (between 0 and 1).</param>
        /// <returns>Eased value.</returns>
        public static Vector2 Ease(Mode mode, Vector2 start, Vector2 end, float value)
        {
            switch (mode)
            {
                case Mode.Linear: return Linear(start, end, value);
                case Mode.Clerp: return Clerp(start, end, value);
                case Mode.Spring: return Spring(start, end, value);
                case Mode.EaseInQuad: return EaseInQuad(start, end, value);
                case Mode.EaseOutQuad: return EaseOutQuad(start, end, value);
                case Mode.EaseInOutQuad: return EaseInOutQuad(start, end, value);
                case Mode.EaseInCubic: return EaseInCubic(start, end, value);
                case Mode.EaseOutCubic: return EaseOutCubic(start, end, value);
                case Mode.EaseInOutCubic: return EaseInOutCubic(start, end, value);
                case Mode.EaseInQuart: return EaseInQuart(start, end, value);
                case Mode.EaseOutQuart: return EaseOutQuart(start, end, value);
                case Mode.EaseInOutQuart: return EaseInOutQuart(start, end, value);
                case Mode.EaseInQuint: return EaseInQuint(start, end, value);
                case Mode.EaseOutQuint: return EaseOutQuint(start, end, value);
                case Mode.EaseInOutQuint: return EaseInOutQuint(start, end, value);
                case Mode.EaseInSine: return EaseInSine(start, end, value);
                case Mode.EaseOutSine: return EaseOutSine(start, end, value);
                case Mode.EaseInOutSine: return EaseInOutSine(start, end, value);
                case Mode.EaseInExpo: return EaseInExpo(start, end, value);
                case Mode.EaseOutExpo: return EaseOutExpo(start, end, value);
                case Mode.EaseOutExpoOver: return EaseOutExpoOver(start, end, value);
                case Mode.EaseInOutExpo: return EaseInOutExpo(start, end, value);
                case Mode.EaseInCirc: return EaseInCirc(start, end, value);
                case Mode.EaseOutCirc: return EaseOutCirc(start, end, value);
                case Mode.EaseInOutCirc: return EaseInOutCirc(start, end, value);
                case Mode.EaseInBounce: return EaseInBounce(start, end, value);
                case Mode.EaseOutBounce: return EaseOutBounce(start, end, value);
                case Mode.EaseInOutBounce: return EaseInOutBounce(start, end, value);
                case Mode.EaseInBack: return EaseInBack(start, end, value);
                case Mode.EaseOutBack: return EaseOutBack(start, end, value);
                case Mode.EaseInOutBack: return EaseInOutBack(start, end, value);
                case Mode.Punch: return Punch(start, value); // Assuming 'start' is used as amplitude here
                case Mode.EaseInElastic: return EaseInElastic(start, end, value);
                case Mode.EaseOutElastic: return EaseOutElastic(start, end, value);
                case Mode.EaseInOutElastic: return EaseInOutElastic(start, end, value);
                default: return Linear(start, end, value); // Default to linear interpolation
            }
        }

		/// <summary>
        /// Applies easing to a value based on the provided mode.
        /// </summary>
        /// <param name="mode">Easing mode to use.</param>
        /// <param name="start">Start value.</param>
        /// <param name="end">End value.</param>
        /// <param name="value">Interpolation value (between 0 and 1).</param>
        /// <returns>Eased value.</returns>
        public static Vector3 Ease(Mode mode, Vector3 start, Vector3 end, float value)
        {
            switch (mode)
            {
                case Mode.Linear: return Linear(start, end, value);
                case Mode.Clerp: return Clerp(start, end, value);
                case Mode.Spring: return Spring(start, end, value);
                case Mode.EaseInQuad: return EaseInQuad(start, end, value);
                case Mode.EaseOutQuad: return EaseOutQuad(start, end, value);
                case Mode.EaseInOutQuad: return EaseInOutQuad(start, end, value);
                case Mode.EaseInCubic: return EaseInCubic(start, end, value);
                case Mode.EaseOutCubic: return EaseOutCubic(start, end, value);
                case Mode.EaseInOutCubic: return EaseInOutCubic(start, end, value);
                case Mode.EaseInQuart: return EaseInQuart(start, end, value);
                case Mode.EaseOutQuart: return EaseOutQuart(start, end, value);
                case Mode.EaseInOutQuart: return EaseInOutQuart(start, end, value);
                case Mode.EaseInQuint: return EaseInQuint(start, end, value);
                case Mode.EaseOutQuint: return EaseOutQuint(start, end, value);
                case Mode.EaseInOutQuint: return EaseInOutQuint(start, end, value);
                case Mode.EaseInSine: return EaseInSine(start, end, value);
                case Mode.EaseOutSine: return EaseOutSine(start, end, value);
                case Mode.EaseInOutSine: return EaseInOutSine(start, end, value);
                case Mode.EaseInExpo: return EaseInExpo(start, end, value);
                case Mode.EaseOutExpo: return EaseOutExpo(start, end, value);
                case Mode.EaseOutExpoOver: return EaseOutExpoOver(start, end, value);
                case Mode.EaseInOutExpo: return EaseInOutExpo(start, end, value);
                case Mode.EaseInCirc: return EaseInCirc(start, end, value);
                case Mode.EaseOutCirc: return EaseOutCirc(start, end, value);
                case Mode.EaseInOutCirc: return EaseInOutCirc(start, end, value);
                case Mode.EaseInBounce: return EaseInBounce(start, end, value);
                case Mode.EaseOutBounce: return EaseOutBounce(start, end, value);
                case Mode.EaseInOutBounce: return EaseInOutBounce(start, end, value);
                case Mode.EaseInBack: return EaseInBack(start, end, value);
                case Mode.EaseOutBack: return EaseOutBack(start, end, value);
                case Mode.EaseInOutBack: return EaseInOutBack(start, end, value);
                case Mode.Punch: return Punch(start, value); // Assuming 'start' is used as amplitude here
                case Mode.EaseInElastic: return EaseInElastic(start, end, value);
                case Mode.EaseOutElastic: return EaseOutElastic(start, end, value);
                case Mode.EaseInOutElastic: return EaseInOutElastic(start, end, value);
                default: return Linear(start, end, value); // Default to linear interpolation
            }
        }

		#region Float Easing
		/// <summary>
		/// Linear the specified start, end and value.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float Linear(float start, float end, float value){
			return Mathf.Lerp(start, end, value);
		}
		/// <summary>
		/// Clerp easing function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float Clerp(float start, float end, float value){
			float min = 0.0f;
			float max = 360.0f;
			float half = Mathf.Abs((max - min) * 0.5f);
			float retval = 0.0f;
			float diff = 0.0f;
			if ((end - start) < -half){
				diff = ((max - start) + end) * value;
				retval = start + diff;
			}else if ((end - start) > half){
				diff = -((max - end) + start) * value;
				retval = start + diff;
			}else retval = start + (end - start) * value;
			return retval;
		}
		/// <summary>
		/// Spring easing funtion.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float Spring(float start, float end, float value){
			value = Mathf.Clamp01(value);
			value = (Mathf.Sin(value * Mathf.PI * (0.2f + 2.5f * value * value * value)) * Mathf.Pow(1f - value, 2.2f) + value) * (1f + (1.2f * (1f - value)));
			return start + (end - start) * value;
		}
		/// <summary>
		/// EaseInQuad function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInQuad(float start, float end, float value){
			end -= start;
			return end * value * value + start;
		}
		/// <summary>
		/// Eases the out quad.
		/// </summary>
		/// <returns>The out quad.</returns>
		/// <param name="start">Start.</param>
		/// <param name="end">End.</param>
		/// <param name="value">Value.</param>
		public static float EaseOutQuad(float start, float end, float value){
			end -= start;
			return -end * value * (value - 2) + start;
		}
		/// <summary>
		/// EaseInOutQuad function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInOutQuad(float start, float end, float value){
			value /= .5f;
			end -= start;
			if (value < 1) return end * 0.5f * value * value + start;
			value--;
			return -end * 0.5f * (value * (value - 2) - 1) + start;
		}
		/// <summary>
		/// EaseInCubic function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInCubic(float start, float end, float value){
			end -= start;
			return end * value * value * value + start;
		}
		/// <summary>
		/// EaseOutCubic function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseOutCubic(float start, float end, float value){
			value--;
			end -= start;
			return end * (value * value * value + 1) + start;
		}
		/// <summary>
		/// EaseInOutCubic function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInOutCubic(float start, float end, float value){
			value /= .5f;
			end -= start;
			if (value < 1) return end * 0.5f * value * value * value + start;
			value -= 2;
			return end * 0.5f * (value * value * value + 2) + start;
		}
		/// <summary>
		/// EaseInQuart function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInQuart(float start, float end, float value){
			end -= start;
			return end * value * value * value * value + start;
		}
		/// <summary>
		/// EaseOutQuart function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseOutQuart(float start, float end, float value){
			value--;
			end -= start;
			return -end * (value * value * value * value - 1) + start;
		}
		/// <summary>
		/// EaseInOutQuart function
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInOutQuart(float start, float end, float value){
			value /= .5f;
			end -= start;
			if (value < 1) return end * 0.5f * value * value * value * value + start;
			value -= 2;
			return -end * 0.5f * (value * value * value * value - 2) + start;
		}
		/// <summary>
		/// EaseInQuint function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInQuint(float start, float end, float value){
			end -= start;
			return end * value * value * value * value * value + start;
		}
		/// <summary>
		/// EaseOutQuint funtion.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseOutQuint(float start, float end, float value){
			value--;
			end -= start;
			return end * (value * value * value * value * value + 1) + start;
		}
		/// <summary>
		/// EaseInOutQuint function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInOutQuint(float start, float end, float value){
			value /= .5f;
			end -= start;
			if (value < 1) return end * 0.5f * value * value * value * value * value + start;
			value -= 2;
			return end * 0.5f * (value * value * value * value * value + 2) + start;
		}
		/// <summary>
		/// EaseInSine function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInSine(float start, float end, float value){
			end -= start;
			return -end * Mathf.Cos(value * (Mathf.PI * 0.5f)) + end + start;
		}
		/// <summary>
		/// EaseOutSine function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseOutSine(float start, float end, float value){
			end -= start;
			return end * Mathf.Sin(value * (Mathf.PI * 0.5f)) + start;
		}
		/// <summary>
		/// EaseInOutSine function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInOutSine(float start, float end, float value){
			end -= start;
			return -end * 0.5f * (Mathf.Cos(Mathf.PI * value) - 1) + start;
		}
		/// <summary>
		/// EaseInExpo function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInExpo(float start, float end, float value){
			end -= start;
			return end * Mathf.Pow(2, 10 * (value - 1)) + start;
		}
		/// <summary>
		/// EaseOutExpo function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseOutExpo(float start, float end, float value){
			end -= start;
			return end * (-Mathf.Pow(2, -10 * value ) + 1) + start;
		}
		/// <summary>
		/// EaseOutExpoOver function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseOutExpoOver(float start, float end, float value){
			end -= start;
			return end * 1.2f * (-Mathf.Pow(2, -2.5f * value ) + 1) + start;
		}
		/// <summary>
		/// EaseInOutExpo function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInOutExpo(float start, float end, float value){
			value /= .5f;
			end -= start;
			if (value < 1) return end * 0.5f * Mathf.Pow(2, 10 * (value - 1)) + start;
			value--;
			return end * 0.5f * (-Mathf.Pow(2, -10 * value) + 2) + start;
		}
		/// <summary>
		/// EaseInCirc function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInCirc(float start, float end, float value){
			end -= start;
			return -end * (Mathf.Sqrt(1 - value * value) - 1) + start;
		}
		/// <summary>
		/// EaseOutCirc function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseOutCirc(float start, float end, float value){
			value--;
			end -= start;
			return end * Mathf.Sqrt(1 - value * value) + start;
		}
		/// <summary>
		/// EaseInOutCirc function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInOutCirc(float start, float end, float value){
			value /= .5f;
			end -= start;
			if (value < 1) return -end * 0.5f * (Mathf.Sqrt(1 - value * value) - 1) + start;
			value -= 2;
			return end * 0.5f * (Mathf.Sqrt(1 - value * value) + 1) + start;
		}
		/// <summary>
		/// EaseInBounce function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInBounce(float start, float end, float value){
			end -= start;
			float d = 1f;
			return end - EaseOutBounce(0, end, d-value) + start;
		}
		/// <summary>
		/// EaseOutBounce function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseOutBounce(float start, float end, float value){
			value /= 1f;
			end -= start;
			if (value < (1 / 2.75f)){
				return end * (7.5625f * value * value) + start;
			}else if (value < (2 / 2.75f)){
				value -= (1.5f / 2.75f);
				return end * (7.5625f * (value) * value + .75f) + start;
			}else if (value < (2.5 / 2.75)){
				value -= (2.25f / 2.75f);
				return end * (7.5625f * (value) * value + .9375f) + start;
			}else{
				value -= (2.625f / 2.75f);
				return end * (7.5625f * (value) * value + .984375f) + start;
			}
		}
		/// <summary>
		/// EaseInOutBounce function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInOutBounce(float start, float end, float value){
			end -= start;
			float d = 1f;
			if (value < d* 0.5f) return EaseInBounce(0, end, value*2) * 0.5f + start;
			else return EaseOutBounce(0, end, value*2-d) * 0.5f + end*0.5f + start;
		}
		/// <summary>
		/// EaseInBack function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInBack(float start, float end, float value){
			end -= start;
			value /= 1;
			float s = 1.70158f;
			return end * (value) * value * ((s + 1) * value - s) + start;
		}
		/// <summary>
		/// EaseOutBack function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseOutBack(float start, float end, float value){
			float s = 1.70158f;
			end -= start;
			value = (value) - 1;
			return end * ((value) * value * ((s + 1) * value + s) + 1) + start;
		}
		/// <summary>
		/// EaseInOutBack function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInOutBack(float start, float end, float value){
			float s = 1.70158f;
			end -= start;
			value /= .5f;
			if ((value) < 1){
				s *= (1.525f);
				return end * 0.5f * (value * value * (((s) + 1) * value - s)) + start;
			}
			value -= 2;
			s *= (1.525f);
			return end * 0.5f * ((value) * value * (((s) + 1) * value + s) + 2) + start;
		}
		/// <summary>
		/// Punch function.
		/// </summary>
		/// <param name="amplitude">Amplitude.</param>
		/// <param name="value">Value.</param>
		public static float Punch(float amplitude, float value){
			float s = 9;
			if (value == 0){
				return 0;
			}
			else if (value == 1){
				return 0;
			}
			float period = 1 * 0.3f;
			s = period / (2 * Mathf.PI) * Mathf.Asin(0);
			return (amplitude * Mathf.Pow(2, -10 * value) * Mathf.Sin((value * 1 - s) * (2 * Mathf.PI) / period));
		}
		/// <summary>
		/// EaseInElastic function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInElastic(float start, float end, float value){
			end -= start;

			float d = 1f;
			float p = d * .3f;
			float s = 0;
			float a = 0;

			if (value == 0) return start;

			if ((value /= d) == 1) return start + end;

			if (a == 0f || a < Mathf.Abs(end)){
				a = end;
				s = p / 4;
			}else{
				s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
			}

			return -(a * Mathf.Pow(2, 10 * (value-=1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p)) + start;
		}		
		/// <summary>
		/// EaseOutElastic function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseOutElastic(float start, float end, float value) {
			end -= start;

			float d = 1f;
			float p = d * .3f;
			float s = 0;
			float a = 0;

			if (value == 0) return start;

			if ((value /= d) == 1) return start + end;

			if (a == 0f || a < Mathf.Abs(end)){
				a = end;
				s = p * 0.25f;
			}else{
				s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
			}

			return (a * Mathf.Pow(2, -10 * value) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p) + end + start);
		}		
		/// <summary>
		/// EaseInOutElastic function.
		/// </summary>
		/// <returns>The ease value.</returns>
		/// <param name="start">Start value.</param>
		/// <param name="end">End value.</param>
		/// <param name="value">Value in time (0, 1).</param>
		public static float EaseInOutElastic(float start, float end, float value) {
			end -= start;

			float d = 1f;
			float p = d * .3f;
			float s = 0;
			float a = 0;

			if (value == 0) return start;

			if ((value /= d*0.5f) == 2) return start + end;

			if (a == 0f || a < Mathf.Abs(end)){
				a = end;
				s = p / 4;
			}else{
				s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
			}

			if (value < 1) return -0.5f * (a * Mathf.Pow(2, 10 * (value-=1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p)) + start;
			return a * Mathf.Pow(2, -10 * (value-=1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p) * 0.5f + end + start;
		}
		#endregion

		#region Vector2 Easing
		public static Vector2 Linear(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				Linear(start.x, end.x, value),
				Linear(start.y, end.y, value)
			);
		}

		public static Vector2 Clerp(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				Clerp(start.x, end.x, value),
				Clerp(start.y, end.y, value)
			);
		}

		public static Vector2 Spring(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				Spring(start.x, end.x, value),
				Spring(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInQuad(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInQuad(start.x, end.x, value),
				EaseInQuad(start.y, end.y, value)
			);
		}

		public static Vector2 EaseOutQuad(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseOutQuad(start.x, end.x, value),
				EaseOutQuad(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInOutQuad(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInOutQuad(start.x, end.x, value),
				EaseInOutQuad(start.y, end.y, value)
			);
		}
		public static Vector2 EaseInCubic(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInCubic(start.x, end.x, value),
				EaseInCubic(start.y, end.y, value)
			);
		}

		public static Vector2 EaseOutCubic(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseOutCubic(start.x, end.x, value),
				EaseOutCubic(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInOutCubic(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInOutCubic(start.x, end.x, value),
				EaseInOutCubic(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInQuart(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInQuart(start.x, end.x, value),
				EaseInQuart(start.y, end.y, value)
			);
		}

		public static Vector2 EaseOutQuart(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseOutQuart(start.x, end.x, value),
				EaseOutQuart(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInOutQuart(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInOutQuart(start.x, end.x, value),
				EaseInOutQuart(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInQuint(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInQuint(start.x, end.x, value),
				EaseInQuint(start.y, end.y, value)
			);
		}

		public static Vector2 EaseOutQuint(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseOutQuint(start.x, end.x, value),
				EaseOutQuint(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInOutQuint(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInOutQuint(start.x, end.x, value),
				EaseInOutQuint(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInSine(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInSine(start.x, end.x, value),
				EaseInSine(start.y, end.y, value)
			);
		}

		public static Vector2 EaseOutSine(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseOutSine(start.x, end.x, value),
				EaseOutSine(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInOutSine(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInOutSine(start.x, end.x, value),
				EaseInOutSine(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInExpo(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInExpo(start.x, end.x, value),
				EaseInExpo(start.y, end.y, value)
			);
		}

		public static Vector2 EaseOutExpo(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseOutExpo(start.x, end.x, value),
				EaseOutExpo(start.y, end.y, value)
			);
		}

		public static Vector2 EaseOutExpoOver(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseOutExpoOver(start.x, end.x, value),
				EaseOutExpoOver(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInOutExpo(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInOutExpo(start.x, end.x, value),
				EaseInOutExpo(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInCirc(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInCirc(start.x, end.x, value),
				EaseInCirc(start.y, end.y, value)
			);
		}

		public static Vector2 EaseOutCirc(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseOutCirc(start.x, end.x, value),
				EaseOutCirc(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInOutCirc(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInOutCirc(start.x, end.x, value),
				EaseInOutCirc(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInBounce(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInBounce(start.x, end.x, value),
				EaseInBounce(start.y, end.y, value)
			);
		}

		public static Vector2 EaseOutBounce(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseOutBounce(start.x, end.x, value),
				EaseOutBounce(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInOutBounce(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInOutBounce(start.x, end.x, value),
				EaseInOutBounce(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInBack(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInBack(start.x, end.x, value),
				EaseInBack(start.y, end.y, value)
			);
		}

		public static Vector2 EaseOutBack(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseOutBack(start.x, end.x, value),
				EaseOutBack(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInOutBack(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInOutBack(start.x, end.x, value),
				EaseInOutBack(start.y, end.y, value)
			);
		}

		public static Vector2 Punch(Vector2 amplitude, float value) {
			return new Vector2 (
				Punch (amplitude.x, value),
				Punch (amplitude.y, value)
			);
		}

		public static Vector2 EaseInElastic(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInElastic(start.x, end.x, value),
				EaseInElastic(start.y, end.y, value)
			);
		}

		public static Vector2 EaseOutElastic(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseOutElastic(start.x, end.x, value),
				EaseOutElastic(start.y, end.y, value)
			);
		}

		public static Vector2 EaseInOutElastic(Vector2 start, Vector2 end, float value)
		{
			return new Vector2(
				EaseInOutElastic(start.x, end.x, value),
				EaseInOutElastic(start.y, end.y, value)
			);
		}
		#endregion

		#region Vector3 Methods
		public static Vector3 Linear(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				Linear(start.x, end.x, value),
				Linear(start.y, end.y, value),
				Linear(start.z, end.z, value)
			);
		}
		public static Vector3 Clerp(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				Clerp(start.x, end.x, value),
				Clerp(start.y, end.y, value),
				Clerp(start.z, end.z, value)
			);
		}

		public static Vector3 Spring(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				Spring(start.x, end.x, value),
				Spring(start.y, end.y, value),
				Spring(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInQuad(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInQuad(start.x, end.x, value),
				EaseInQuad(start.y, end.y, value),
				EaseInQuad(start.z, end.z, value)
			);
		}

		public static Vector3 EaseOutQuad(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseOutQuad(start.x, end.x, value),
				EaseOutQuad(start.y, end.y, value),
				EaseOutQuad(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInOutQuad(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInOutQuad(start.x, end.x, value),
				EaseInOutQuad(start.y, end.y, value),
				EaseInOutQuad(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInCubic(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInCubic(start.x, end.x, value),
				EaseInCubic(start.y, end.y, value),
				EaseInCubic(start.z, end.z, value)
			);
		}

		public static Vector3 EaseOutCubic(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseOutCubic(start.x, end.x, value),
				EaseOutCubic(start.y, end.y, value),
				EaseOutCubic(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInOutCubic(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInOutCubic(start.x, end.x, value),
				EaseInOutCubic(start.y, end.y, value),
				EaseInOutCubic(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInQuart(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInQuart(start.x, end.x, value),
				EaseInQuart(start.y, end.y, value),
				EaseInQuart(start.z, end.z, value)
			);
		}

		public static Vector3 EaseOutQuart(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseOutQuart(start.x, end.x, value),
				EaseOutQuart(start.y, end.y, value),
				EaseOutQuart(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInOutQuart(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInOutQuart(start.x, end.x, value),
				EaseInOutQuart(start.y, end.y, value),
				EaseInOutQuart(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInQuint(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInQuint(start.x, end.x, value),
				EaseInQuint(start.y, end.y, value),
				EaseInQuint(start.z, end.z, value)
			);
		}

		public static Vector3 EaseOutQuint(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseOutQuint(start.x, end.x, value),
				EaseOutQuint(start.y, end.y, value),
				EaseOutQuint(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInOutQuint(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInOutQuint(start.x, end.x, value),
				EaseInOutQuint(start.y, end.y, value),
				EaseInOutQuint(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInSine(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInSine(start.x, end.x, value),
				EaseInSine(start.y, end.y, value),
				EaseInSine(start.z, end.z, value)
			);
		}

		public static Vector3 EaseOutSine(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseOutSine(start.x, end.x, value),
				EaseOutSine(start.y, end.y, value),
				EaseOutSine(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInOutSine(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInOutSine(start.x, end.x, value),
				EaseInOutSine(start.y, end.y, value),
				EaseInOutSine(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInExpo(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInExpo(start.x, end.x, value),
				EaseInExpo(start.y, end.y, value),
				EaseInExpo(start.z, end.z, value)
			);
		}

		public static Vector3 EaseOutExpo(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseOutExpo(start.x, end.x, value),
				EaseOutExpo(start.y, end.y, value),
				EaseOutExpo(start.z, end.z, value)
			);
		}

		public static Vector3 EaseOutExpoOver(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseOutExpoOver(start.x, end.x, value),
				EaseOutExpoOver(start.y, end.y, value),
				EaseOutExpoOver(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInOutExpo(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInOutExpo(start.x, end.x, value),
				EaseInOutExpo(start.y, end.y, value),
				EaseInOutExpo(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInCirc(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInCirc(start.x, end.x, value),
				EaseInCirc(start.y, end.y, value),
				EaseInCirc(start.z, end.z, value)
			);
		}

		public static Vector3 EaseOutCirc(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseOutCirc(start.x, end.x, value),
				EaseOutCirc(start.y, end.y, value),
				EaseOutCirc(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInOutCirc(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInOutCirc(start.x, end.x, value),
				EaseInOutCirc(start.y, end.y, value),
				EaseInOutCirc(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInBounce(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInBounce(start.x, end.x, value),
				EaseInBounce(start.y, end.y, value),
				EaseInBounce(start.z, end.z, value)
			);
		}

		public static Vector3 EaseOutBounce(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseOutBounce(start.x, end.x, value),
				EaseOutBounce(start.y, end.y, value),
				EaseOutBounce(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInOutBounce(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInOutBounce(start.x, end.x, value),
				EaseInOutBounce(start.y, end.y, value),
				EaseInOutBounce(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInBack(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInBack(start.x, end.x, value),
				EaseInBack(start.y, end.y, value),
				EaseInBack(start.z, end.z, value)
			);
		}

		public static Vector3 EaseOutBack(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseOutBack(start.x, end.x, value),
				EaseOutBack(start.y, end.y, value),
				EaseOutBack(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInOutBack(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInOutBack(start.x, end.x, value),
				EaseInOutBack(start.y, end.y, value),
				EaseInOutBack(start.z, end.z, value)
			);
		}

		// Punch method remains the same as it only takes a float value

		public static Vector3 EaseInElastic(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInElastic(start.x, end.x, value),
				EaseInElastic(start.y, end.y, value),
				EaseInElastic(start.z, end.z, value)
			);
		}

		public static Vector3 EaseOutElastic(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseOutElastic(start.x, end.x, value),
				EaseOutElastic(start.y, end.y, value),
				EaseOutElastic(start.z, end.z, value)
			);
		}

		public static Vector3 EaseInOutElastic(Vector3 start, Vector3 end, float value)
		{
			return new Vector3(
				EaseInOutElastic(start.x, end.x, value),
				EaseInOutElastic(start.y, end.y, value),
				EaseInOutElastic(start.z, end.z, value)
			);
		}
		/// <summary>
		/// Interpolates between startValue and endValue but snaps the result to
		/// one of a specified number of discrete, evenly spaced steps.
		/// </summary>
		/// <param name="startValue">The value when the interpolation factor t = 0.</param>
		/// <param name="endValue">The value when the interpolation factor t = 1.</param>
		/// <param name="numberOfSteps">The total number of distinct values in the result (including start and end). Must be 2 or greater for steps.</param>
		/// <param name="t">The interpolation factor, typically ranging from 0.0 to 1.0. Will be clamped.</param>
		/// <returns>The interpolated value snapped to the nearest lower step boundary.</returns>
		public static float LerpSteps (float startValue, float endValue, int numberOfSteps, float t)
		{
			// Ensure t is within the valid 0-1 range
			t = Mathf.Clamp01(t);

			// Need at least 2 steps (start and end) for stepped interpolation to make sense
			if (numberOfSteps < 2)
			{
				// If fewer than 2 steps, behave like regular Lerp at boundaries or just return start
				return (t < 0.5f) ? startValue : endValue;
				// Or simply: return startValue;
			}

			// 1. Determine which step interval 't' falls into.
			// There are (numberOfSteps - 1) intervals between the steps.
			// Example: 5 steps -> values at indices 0, 1, 2, 3, 4. 4 intervals.
			// Multiply t by the number of *intervals* to find the continuous position.
			// float continuousStep = t * (numberOfSteps - 1);

			// Alternative calculation: Find which step bucket 't' belongs to.
			// Example: 5 steps. Buckets are [0..0.2), [0.2..0.4), [0.4..0.6), [0.6..0.8), [0.8..1.0] (approx)
			// Multiply 't' by total steps, floor it, then clamp.
			int stepIndex = Mathf.FloorToInt(t * numberOfSteps);

			// Clamp index to handle t=1 correctly (Floor(1.0 * 5) = 5, needs clamping to max index 4)
			stepIndex = Mathf.Clamp(stepIndex, 0, numberOfSteps - 1);

			// 2. Calculate the normalized progress factor FOR THAT STEP.
			// The interpolation factor for step 'i' is i / (total intervals).
			float stepT = (float)stepIndex / (numberOfSteps - 1);

			// 3. Lerp using the step's normalized factor.
			float steppedValue = Mathf.Lerp(startValue, endValue, stepT);

			return steppedValue;
		}
		/// <summary>
		/// Interpolates smoothly between startValue and endValue following a base linear path
		/// which is modulated by a continuous sine wave. Produces a smooth, curved output.
		/// </summary>
		/// <param name="startValue">The underlying start value (center of wave at t=0).</param>
		/// <param name="endValue">The underlying end value (center of wave at t=1).</param>
		/// <param name="waveAmplitude">The amplitude of the sine wave offset from the base lerped value.</param>
		/// <param name="waveCycles">How many full sine wave cycles should complete between t=0 and t=1.</param>
		/// <param name="t">The overall interpolation factor (0 to 1), will be clamped.</param>
		/// <returns>The smoothly interpolated value including the sinusoidal offset at the current t.</returns>
		public static float LerpSmoothWave(
			float startValue,
			float endValue,
			float waveAmplitude,
			float waveCycles,
			float t)
		{
			// Clamp t to ensure it's within the valid range [0, 1]
			t = Mathf.Clamp01(t);

			// 1. Calculate the base linear value at the current continuous t
			float baseValue = Mathf.Lerp(startValue, endValue, t);

			// 2. Calculate the sine wave offset at the current continuous t
			// Multiply by 2*PI to convert cycles to radians
			float angle = t * waveCycles * 2.0f * Mathf.PI;
			float waveOffset = waveAmplitude * Mathf.Sin(angle);

			// 3. Combine the base value and the wave offset
			float finalValue = baseValue + waveOffset;

			return finalValue;
		}
		#endregion
	}
}