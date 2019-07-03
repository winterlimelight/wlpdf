using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wlpdf.Types;

namespace Wlpdf.Examples
{
    public class Text
    {
        public static void AddTextStandardFont()
        {
            var pdf = PdfDocument.FromFile("../../../../../pdfs/sample.pdf"); // PdfDocument.FromFile("../../../../../pdfs/Sibelius8.pdf");

            var fText = new Font("/Type1", "/Helvetica");
            var helveticaId = pdf.AddObject(fText).ObjectNumber;

            var objects = pdf.GetObjects();
            var pages = objects.Where(o => o.Object is Page).Select(io => io.Object as Page);
            foreach (var page in pages)
            {
                string content = "q\n" + page.Contents.ToString() + "Q\n"; // use q/Q to clear any graphics-state/co-ordinate transforms
                content += $@"
q
/CSp cs 
0 0 0 scn
/GSa gs
BT
/F{helveticaId} 8 Tf
0 830 Td
(omg omg omg) Tj
ET
Q
";
                page.Contents.SetContent(content);
            }

            pdf.Save("out.pdf");
        }
    }
}