using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBus
{
   class OSHelpers
   {

      static PlatformID platformid = Environment.OSVersion.Platform;

      public static bool PlatformIsUnixoid
      {
         get
         {
            switch (platformid)
            {
               case PlatformID.Win32S:       return false;
               case PlatformID.Win32Windows: return false;
               case PlatformID.Win32NT:      return false;
               case PlatformID.WinCE:        return false;
               case PlatformID.Unix:         return true;
               case PlatformID.Xbox:         return false;
               case PlatformID.MacOSX:       return true;
               default:                      return false;
            }
         }
      }

   }
}
