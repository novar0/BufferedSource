using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusinessClassLibrary.Test
{
	[TestClass]
	public class BufferedSourceExtensionsTests
	{
		[TestMethod]
		[TestCategory ("BufferedSource")]
		public void BufferedSourceExtensions_IsEmpty ()
		{
			var src = new BigBufferedSourceMock (0, 1, FillFunction);
			src.FillBuffer ();
			Assert.IsTrue (BufferedSourceExtensions.IsEmpty (src));

			src = new BigBufferedSourceMock (1, 1, FillFunction);
			src.FillBuffer ();
			Assert.IsFalse (BufferedSourceExtensions.IsEmpty (src));
			src.SkipBuffer (1);
			Assert.IsTrue (BufferedSourceExtensions.IsEmpty (src));

			src = new BigBufferedSourceMock (long.MaxValue, 32768, FillFunction);
			src.FillBuffer ();
			Assert.IsFalse (BufferedSourceExtensions.IsEmpty (src));
			src.TrySkip (long.MaxValue - 1);
			src.FillBuffer ();
			Assert.IsFalse (BufferedSourceExtensions.IsEmpty (src));
			src.SkipBuffer (1);
			Assert.IsTrue (BufferedSourceExtensions.IsEmpty (src));
		}

		[TestMethod]
		[TestCategory ("BufferedSource")]
		public void BufferedSourceExtensions_Read ()
		{
			int srcBufSize = 32768;
			int testSampleSize = 68;
			int readBufSize = 1000;
			int readBufOffset = 512;
			var src = new BigBufferedSourceMock (long.MaxValue, srcBufSize, FillFunction);
			src.FillBuffer ();
			var skip = srcBufSize - testSampleSize;
			src.SkipBuffer (skip);
			var buf = new byte[readBufSize];
			Assert.AreEqual (testSampleSize, BufferedSourceExtensions.Read (src, buf, readBufOffset, testSampleSize));
			for (int i = 0; i < testSampleSize; i++)
			{
				Assert.AreEqual (FillFunction ((long)(skip + i)), buf[readBufOffset + i]);
			}
			src.TrySkip (long.MaxValue - (long)srcBufSize - 3);
			Assert.AreEqual (3, BufferedSourceExtensions.Read (src, buf, 0, buf.Length));
		}

		[TestMethod]
		[TestCategory ("BufferedSource")]
		public void BufferedSourceExtensions_ReadAll ()
		{
			int testSampleSize = 1163;
			long skipSize = long.MaxValue - (long)testSampleSize;

			// чтение больше буфера
			int srcBufSize = testSampleSize / 3;
			var src = new BigBufferedSourceMock (long.MaxValue, srcBufSize, FillFunction);
			src.TrySkip (skipSize);
			var result = BufferedSourceExtensions.ReadAllBytes (src);
			for (int i = 0; i < testSampleSize; i++)
			{
				Assert.AreEqual (FillFunction ((long)(skipSize + i)), result[i]);
			}

			// чтение меньше буфера
			srcBufSize = testSampleSize * 9;
			src = new BigBufferedSourceMock (long.MaxValue, srcBufSize, FillFunction);
			src.TrySkip (skipSize);
			result = BufferedSourceExtensions.ReadAllBytes (src);
			for (int i = 0; i < testSampleSize; i++)
			{
				Assert.AreEqual (FillFunction ((long)(skipSize + i)), result[i]);
			}
		}

		[TestMethod]
		[TestCategory ("BufferedSource")]
		public void BufferedSourceExtensions_WriteTo ()
		{
			int testSampleSize = 1163;
			long skipSize = long.MaxValue - (long)testSampleSize;
			int srcBufSize = testSampleSize / 3;
			var src = new BigBufferedSourceMock (long.MaxValue, srcBufSize, FillFunction);
			src.TrySkip (skipSize);
			byte[] result;
			using (var dst = new MemoryStream ())
			{
				Assert.AreEqual (testSampleSize, BufferedSourceExtensions.WriteTo (src, dst));
				result = dst.GetBuffer ();
			}
			for (int i = 0; i < testSampleSize; i++)
			{
				Assert.AreEqual (FillFunction ((long)(skipSize + i)), result[i]);
			}
		}

		private static byte FillFunction (long position)
		{
			return (byte)(0xAA ^ (position & 0xFF));
		}
	}
}
