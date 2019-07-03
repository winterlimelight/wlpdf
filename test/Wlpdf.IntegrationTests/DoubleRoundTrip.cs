using System;
using System.IO;
using Xunit;
using Wlpdf.Types;

namespace Wlpdf.IntegrationTests
{
    public class DoubleRoundTrip
    {
        [Fact]
        public void DoubleRoundTripAllSamples()
        {
            const string firstFile = "out1.pdf";
            const string secondFile = "out2.pdf";
            Action deleteFiles = () =>
            {
                File.Delete(firstFile);
                File.Delete(secondFile);
                
            };
            deleteFiles();

            var path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../../../pdfs"));
            var pdfs = Directory.EnumerateFiles(path, "*.pdf");
            foreach(var pdf in pdfs)
            {
                var doc = PdfDocument.FromFile(pdf);
                doc.Save(firstFile);

                doc = PdfDocument.FromFile(firstFile);
                doc.Save(secondFile);

                byte [] firstFileBytes = File.ReadAllBytes(firstFile);
                byte[] secondFileBytes = File.ReadAllBytes(secondFile);
                Assert.Equal(firstFileBytes, secondFileBytes);

                deleteFiles();
            }
        }
    }
}
