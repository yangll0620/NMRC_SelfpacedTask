using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GonoGoTask_wpfVer
{
    class Convert2Pixal
    {
        public Convert2Pixal()
        {
            
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

            int ratio = 96;

            int pixalen = (int)(inlen * ratio);

            return pixalen;
        }
    }
}
