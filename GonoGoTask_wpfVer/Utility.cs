using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using swf = System.Windows.Forms;
using sd = System.Drawing; 

namespace GonoGoTask_wpfVer
{
    class Utility
    {
        public Utility()
        { }

        public static swf.Screen Detect_TouchScreen()
        {
            /* Detect the Touch Screen */
            swf.Screen[] screens = swf.Screen.AllScreens;
            int si, touchSi = -1;
            for (si = 0; si < screens.Length; si++)
            {
                swf.Screen s = screens[si];
                if (s.Bounds.Width == 1280 && s.Bounds.Height == 1024)
                {
                    touchSi = si;
                    break;
                }
            }
            if (touchSi == -1)
            {
                touchSi = 0;
            }
            swf.Screen touchScreen = screens[touchSi];
            return touchScreen;
        }

        public static swf.Screen Detect_notTouchScreen()
        {
            /* Detect the first not Touch Screen */
            swf.Screen[] screens = swf.Screen.AllScreens;
            int si, nottouchSi = -1;
            for (si = 0; si < screens.Length; si++)
            {
                swf.Screen s = screens[si];
                if (s.Bounds.Width != 1280 || s.Bounds.Height != 1024)
                {
                    nottouchSi = si;
                    break;
                }
            }
            if (nottouchSi == -1)
            {
                nottouchSi = 0;
            }
            swf.Screen nottouchScreen = screens[nottouchSi];
            return nottouchScreen;
        }
    }
}
