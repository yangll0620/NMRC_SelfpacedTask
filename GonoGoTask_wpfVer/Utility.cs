using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using swf = System.Windows.Forms;
using sd = System.Drawing;
using System.Windows.Media;
namespace COTTask_wpf
{
    class Utility
    {
        static public int ratioIn2Pixal = 96;
        public Utility()
        { }


        public static swf.Screen Detect_oneNonPrimaryScreen()
        {/* Detect the first not Primary Screen */

            swf.Screen[] screens = swf.Screen.AllScreens;
            swf.Screen nonPrimaryS = swf.Screen.PrimaryScreen; 
            foreach (swf.Screen s in screens)
            {
                if(s.Primary == false)
                {
                    nonPrimaryS = s;
                    break;
                }
            }
            return nonPrimaryS;
        }


        public static int cm2pixal(float cmlen)
        {/* convert length with unit cm to unit pixal, 96 pixals = 1 inch = 2.54 cm

            args:   
                cmlen: to be converted length (unit: cm)

            return:
                pixalen: converted length with unit pixal
         */

            float ratio = (float)96 / (float)2.54;

            int pixalen = (int)(cmlen * ratio);

            return pixalen;
        }

        public static int in2pixal(float inlen)
        {/* convert length with unit inch to unit pixal, 96 pixals = 1 inch = 2.54 cm

            args:   
                cmlen: to be converted length (unit: inch)

            return:
                pixalen: converted length with unit pixal
         */     

            int pixalen = (int)(inlen * ratioIn2Pixal);

            return pixalen;
        }
    }
}
