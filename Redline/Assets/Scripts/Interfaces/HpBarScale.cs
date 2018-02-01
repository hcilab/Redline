using UnityEngine;

namespace Interfaces
{
	public abstract class HpBarScale : MonoBehaviour
	{
		/// <summary>
		/// Returns the width of the HP bar given a certain health percentage. 
		/// This is where the visualization of HP state can be tweaked.
		/// </summary>
		/// <param name="percentageHp">HP in percentage of total.</param>
		/// <returns>Width of the HP bar in pixels relative to it's total length.</returns>
		public abstract double scale(double percentageHp);
	}
}
