// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HexaGen.Runtime;
using System.Numerics;

namespace Dalamud.Bindings.ImGui
{
	/// <summary>
	/// To be documented.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public partial struct ImGuiInputEventMouseWheel
	{
		/// <summary>
		/// To be documented.
		/// </summary>
		public float WheelX;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float WheelY;


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImGuiInputEventMouseWheel(float wheelX = default, float wheelY = default)
		{
			WheelX = wheelX;
			WheelY = wheelY;
		}


	}

}
