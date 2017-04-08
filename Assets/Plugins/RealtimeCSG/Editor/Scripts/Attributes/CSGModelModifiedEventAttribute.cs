using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[AttributeUsage(AttributeTargets.Method)]
#if !DEMO
public
#else
internal
#endif
class CSGModelModifiedEventAttribute : Attribute
{
}
