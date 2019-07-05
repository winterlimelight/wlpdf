# wlpdf
PDF Editing Library in .NET Core

This library was developed to allow watermarks to be added to existing PDFs.
It therefore started with loading and saving PDFs, then adding elements to them.

I undertook this rather than using an existing library for two reasons: 
this was a purely amateur project with no revenue involved and quality PDF libraries are expensive (and having delved into the PDF spec a little, I can see why!); 
and I like developing things :)

The src/Wlpdf.Examples project illustrates potential uses of the library.

This has been tested against a few PDFs of mine and from the internet with PDF versions 1.3, 1.4, and 1.5.
There's a very good chance, in the unlikely event someone tries to use this, that their PDF won't load in a reader after loading and saving (a 'round-trip').
In that case, if you have rights to the PDF, I'd be happy to add it to my collection of samples and try to get it running (not all my samples are in the repository as they aren't my PDFs).
There are a great many facets of the PDF format I haven't got into, but one that certainly isn't supported is encryption.