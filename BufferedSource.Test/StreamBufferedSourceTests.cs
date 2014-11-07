using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessClassLibrary.IO;

namespace BusinessClassLibrary.Test
{
	[TestClass]
	public class StreamBufferedSourceTests
	{
		[TestMethod]
		[TestCategory ("BufferedSource")]
		public void StreamBufferedSource_RequestSkip ()
		{
			EmptySourceTest (1);
			EmptySourceTest (2);
			EmptySourceTest (3);
			EmptySourceTest (65536);
			OneByteSourceTest (1);
			OneByteSourceTest (2);
			OneByteSourceTest (3);
			OneByteSourceTest (65536);
			// нулевые пропуски, то есть проверяются все байты
			SkipTest3SkipTest3SkipTest (6, true, 3, 0, 0, 0, 0, 1000);
			SkipTest3SkipTest3SkipTest (6, false, 3, 0, 0, 0, 0, 1000);
			// буфер меньше источника
			SkipTest3SkipTest3SkipTest (65536, true, 10000, 1, 3, 54, 20000, 65536);
			SkipTest3SkipTest3SkipTest (65536, false, 10000, 1, 3, 54, 20000, 65536);
			// буфер больше источника
			SkipTest3SkipTest3SkipTest (10000, true, 65536, 10, 1000, 1, 2000, 10000);
			SkipTest3SkipTest3SkipTest (10000, false, 65536, 10, 1000, 1, 2000, 10000);
			// большой источник
			SkipTest3SkipTest3SkipTest (0x1000000000000, true, 10000, 1, 3, 0x20000000000, 65536, 0x4000000000000000);
			SkipTest3SkipTest3SkipTest (long.MaxValue, true, 10000, 1, 3, 0x2000000000000, 65536, long.MaxValue);
		}

		private static void EmptySourceTest (int bufSize)
		{
			var data = new byte[0];
			var strm = new MemoryStream (data);
			var src = new StreamBufferedSource (strm, new byte[bufSize]);
			Assert.AreEqual (0, src.FillBuffer ());
			Assert.AreEqual (0, src.TrySkip (1));
			Assert.AreEqual (0, src.FillBuffer ());
			Assert.IsTrue (src.IsExhausted);
			Assert.AreEqual (0, src.Count);
		}

		private static void OneByteSourceTest (int bufSize)
		{
			byte nnn = 123;
			var data = new byte[] { nnn };
			var strm = new MemoryStream (data);

			var src = new StreamBufferedSource (strm, new byte[bufSize]);
			src.EnsureBuffer (1);
			Assert.AreEqual (nnn, src.Buffer[src.Offset]);
			src.SkipBuffer (1);
			Assert.AreEqual (0, src.FillBuffer ());
			Assert.IsTrue (src.IsExhausted);
			Assert.AreEqual (0, src.Count);

			strm.Seek (0, SeekOrigin.Begin);
			src = new StreamBufferedSource (strm, new byte[bufSize]);
			Assert.AreEqual (1, src.FillBuffer ());
			Assert.AreEqual (nnn, src.Buffer[src.Offset]);
			Assert.AreEqual (1, src.TrySkip (bufSize));
			Assert.AreEqual (0, src.FillBuffer ());
			Assert.IsTrue (src.IsExhausted);
			Assert.AreEqual (0, src.Count);
		}

		private static byte FillFunction (long position)
		{
			return (byte)(0xAA ^ (position & 0xFF));
		}
		// проверяем 2 блока по 3 байта перед каждым делаем пропуск
		private static void SkipTest3SkipTest3SkipTest (
			long dataSize,
			bool canSeek,
			int bufSize,
			int skipBuffer1,
			int skipBuffer2,
			long skipOverall1,
			long skipOverall2,
			long skipOverEnd)
		{
			System.Diagnostics.Trace.WriteLine (string.Format ("dataSize = {0}, canSeek = {7}, bufSize = {1}, skipOverall1 = {2}, skipOverall2 = {3}, skipOverEnd = {4}, skipBuffer1 = {5}, skipBuffer2 = {6}", dataSize, bufSize, skipOverall1, skipOverall2, skipOverEnd, skipBuffer1, skipBuffer2, canSeek));
			var strm = new BigStreamMock (dataSize, canSeek, FillFunction);
			var src = new StreamBufferedSource (strm, new byte[bufSize]);
			src.EnsureBuffer (3);
			Assert.AreEqual (FillFunction(0), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction(1), src.Buffer[src.Offset + 1]);
			Assert.AreEqual (FillFunction(2), src.Buffer[src.Offset + 2]);
			Assert.AreEqual (skipOverall1, src.TrySkip (skipOverall1));
			src.EnsureBuffer (skipBuffer1);
			src.SkipBuffer (skipBuffer1);
			src.EnsureBuffer (3);
			Assert.AreEqual (FillFunction(skipOverall1 + skipBuffer1), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction(skipOverall1 + skipBuffer1 + 1), src.Buffer[src.Offset + 1]);
			Assert.AreEqual (FillFunction(skipOverall1 + skipBuffer1 + 2), src.Buffer[src.Offset + 2]);
			Assert.AreEqual (skipOverall2, src.TrySkip (skipOverall2));
			src.EnsureBuffer (skipBuffer2);
			src.SkipBuffer (skipBuffer2);
			src.EnsureBuffer (3);
			Assert.AreEqual (FillFunction(skipOverall1 + skipOverall2 + skipBuffer1 + skipBuffer2), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction(skipOverall1 + skipOverall2 + skipBuffer1 + skipBuffer2 + 1), src.Buffer[src.Offset + 1]);
			Assert.AreEqual (FillFunction(skipOverall1 + skipOverall2 + skipBuffer1 + skipBuffer2 + 2), src.Buffer[src.Offset + 2]);
			Assert.AreEqual (dataSize - skipOverall1 - skipOverall2 - skipBuffer1 - skipBuffer2, src.TrySkip (skipOverEnd));
			Assert.IsTrue (src.IsExhausted);
			Assert.AreEqual (0, src.Count);
		}
	}
}
