using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Filters
{
    internal interface IFilter
    {
        byte [] Decode(byte [] bytes);
        byte [] Encode(byte [] bytes);
    }
}
