using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Wlpdf.Types.Basic;

namespace Wlpdf.Types.Common
{
    public class PdfRectangle
    {
        private readonly RectangleF _rect;

        public PdfRectangle(PdfArray ary)
        {
            if (ary.Count != 4)
                throw new ArgumentException();
            var ary2 = ary.Select(a => (float)(a as PdfNumeric)).ToArray();

            float lowerLeftX = ary2[0];
            float lowerLeftY = ary2[1];
            float upperRightX = ary2[2];
            float upperRightY = ary2[3];
            _rect = new RectangleF(lowerLeftX, lowerLeftY, upperRightX - lowerLeftX, upperRightY - lowerLeftY);
        }

        // TODO if settor, then need ToPdfArray()

        public RectangleF Rectangle
        {
            get
            {
                return _rect;
            }
        }
    }
}
