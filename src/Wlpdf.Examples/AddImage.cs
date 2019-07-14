using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using Wlpdf.Types;
using Wlpdf.Types.Basic;

namespace Wlpdf.Examples
{
    public class AddImage
    {
        public static void Add32bppWithTransparency()
        {
            var pdf = PdfDocument.FromFile("../../../../../pdfs/Word2016.pdf");
            var img = Image.FromFile("../../../32bpp.png");

            // create XObject
            var xobj = new XObject();
            xobj.SetImage(img);
            var imgXref = pdf.AddObject(xobj);
            var imgRef = pdf.GetIndirectReference(imgXref);

            // alter content
            var objects = pdf.GetObjects();
            var pages = objects.Where(o => o.Object is Page).Select(io => io.Object as Page);
            foreach (var page in pages)
            {
                // add resource to page
                string imgId = page.AddXObject(imgRef);

                float fromTop = (page.MediaBox.Rectangle.Height / 2) - (img.Height / 2);

                string content = "q\n" + page.Contents.ToString() + "Q\n"; // use q/Q to clear any graphics-state/co-ordinate transforms
                content += $@"
q
{img.Width} 0 0 {img.Height} 0 {fromTop} cm
{imgId} Do
Q
";
                page.Contents.SetContent(content);
            }
            
            pdf.Save("out.pdf");
        }
    }
}