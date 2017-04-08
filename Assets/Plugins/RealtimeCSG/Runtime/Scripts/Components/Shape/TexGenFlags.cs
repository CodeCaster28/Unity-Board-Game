using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealtimeCSG
{
	// mirrored on C++ side
	[Flags]
	public enum TexGenFlags : int //32 bits
	{
		None				= 0,
		WorldSpaceTexture	= 1,
		Discarded			= 2,
		ShadowOnly			= 4,
		HelperSurface		= 1<<30
	};
}
