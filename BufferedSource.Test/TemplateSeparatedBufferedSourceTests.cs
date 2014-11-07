using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusinessClassLibrary.Test
{
	[TestClass]
	public class TemplateSeparatedBufferedSourceTests
	{
		[TestMethod]
		[TestCategory ("BufferedSource")]
		public void TemplateSeparatedBufferedSource_RequestSkipPart ()
		{
			byte templPos = 162;
			var separator = new byte[]
			{
				FillFunction (templPos),
				FillFunction (templPos + 1),
				FillFunction (templPos + 2),
				FillFunction (templPos + 3),
				FillFunction (templPos + 4)
			};
			long skipBeforeLimitingSize = 0xfffffffd;
			long secondPartPos = (skipBeforeLimitingSize | 0xffL) + 1L + (long)templPos + separator.Length;
			int skipBufferSize = 93;
			int srcBufSize = 32768;

			// части в середине источника
			var subSrc = new BigBufferedSourceMock (long.MaxValue, srcBufSize, FillFunction);
			subSrc.TrySkip (skipBeforeLimitingSize);
			var src = new TemplateSeparatedBufferedSource (subSrc, separator);
			src.EnsureBuffer (skipBufferSize + 3);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + 1), src.Buffer[src.Offset + 1]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + 2), src.Buffer[src.Offset + 2]);
			src.SkipBuffer (skipBufferSize);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize + 1), src.Buffer[src.Offset + 1]);
			Assert.AreEqual (FillFunction (skipBeforeLimitingSize + skipBufferSize + 2), src.Buffer[src.Offset + 2]);
			Assert.IsTrue (src.TrySkipPart ());
			src.EnsureBuffer (3);
			Assert.AreEqual (FillFunction (secondPartPos), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction (secondPartPos + 1), src.Buffer[src.Offset + 1]);
			Assert.AreEqual (FillFunction (secondPartPos + 2), src.Buffer[src.Offset + 2]);
			Assert.AreEqual (256 - separator.Length, src.TrySkip (long.MaxValue));

			// части в конце источника
			long size = 4611686018427387904L;
			subSrc = new BigBufferedSourceMock (size, srcBufSize, FillFunction);
			subSrc.TrySkip (size - 256 - 256 - 20); // отступаем так чтобы осталось две части с хвостиком
			src = new TemplateSeparatedBufferedSource (subSrc, separator);
			Assert.IsTrue (src.TrySkipPart ());
			Assert.IsTrue (src.TrySkipPart ());
			Assert.IsFalse (src.TrySkipPart ());

			// разделитель в конце источника
			separator = new byte[]
			{
				FillFunction (253),
				FillFunction (254),
				FillFunction (255)
			};
			subSrc = new BigBufferedSourceMock (768, srcBufSize, FillFunction);
			src = new TemplateSeparatedBufferedSource (subSrc, separator);
			Assert.IsTrue (src.TrySkipPart ());
			Assert.IsTrue (src.TrySkipPart ());
			Assert.IsTrue (src.TrySkipPart ());
			Assert.IsFalse (src.TrySkipPart ());
		}
		private static byte FillFunction (long position)
		{
			return (byte)(0xAA ^ (position & 0xFF));
		}
	}
}
