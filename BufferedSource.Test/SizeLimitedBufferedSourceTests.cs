using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusinessClassLibrary.Test
{
	[TestClass]
	public class SizeLimitedBufferedSourceTests
	{
		[TestMethod]
		[TestCategory ("BufferedSource")]
		public void SizeLimitedBufferedSource_RequestSkip ()
		{
			long skipBeforeLimitingSize = int.MaxValue;
			int skipBufferSize = 123;
			long skipInsideLimitingSize = 562945658454016;

			// ограничение больше буфера
			int srcBufSize = 32768;
			long limitingSize = srcBufSize + 4611686018427387904L;
			var subSrc = new BigBufferedSourceMock (long.MaxValue, srcBufSize, FillFunction);
			subSrc.TrySkip (skipBeforeLimitingSize);

			var src = new SizeLimitedBufferedSource (subSrc, limitingSize);
			src.EnsureBuffer (skipBufferSize + 3);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize+1), src.Buffer[src.Offset+1]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize+2), src.Buffer[src.Offset+2]);
			src.SkipBuffer (skipBufferSize);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize + 1), src.Buffer[src.Offset + 1]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize + 2), src.Buffer[src.Offset + 2]);
			Assert.AreEqual (skipInsideLimitingSize, src.TrySkip (skipInsideLimitingSize));
			src.EnsureBuffer (3);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize + skipInsideLimitingSize), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize + skipInsideLimitingSize + 1), src.Buffer[src.Offset + 1]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize + skipInsideLimitingSize + 2), src.Buffer[src.Offset + 2]);
			Assert.AreEqual (limitingSize - skipBufferSize - skipInsideLimitingSize, src.TrySkip (long.MaxValue));

			// ограничение меньше буфера
			srcBufSize = 32767;
			limitingSize = 1293L;
			subSrc = new BigBufferedSourceMock (long.MaxValue, srcBufSize, FillFunction);
			subSrc.TrySkip (skipBeforeLimitingSize);

			src = new SizeLimitedBufferedSource (subSrc, limitingSize);
			src.EnsureBuffer (skipBufferSize + 3);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + 1), src.Buffer[src.Offset + 1]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + 2), src.Buffer[src.Offset + 2]);
			src.SkipBuffer (skipBufferSize);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize + 1), src.Buffer[src.Offset + 1]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize + 2), src.Buffer[src.Offset + 2]);
			Assert.AreEqual (skipBufferSize, src.TrySkip (skipBufferSize));
			src.EnsureBuffer (3);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize + skipBufferSize), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize + skipBufferSize + 1), src.Buffer[src.Offset + 1]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize + skipBufferSize + 2), src.Buffer[src.Offset + 2]);
			Assert.AreEqual (limitingSize - skipBufferSize - skipBufferSize, src.TrySkip (long.MaxValue));
		}
		private static byte FillFunction (long position)
		{
			return (byte)(0xAA ^ (position & 0xFF));
		}
	}
}
