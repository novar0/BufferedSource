using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ICryptographicTransform = System.Security.Cryptography.ICryptoTransform;

namespace BusinessClassLibrary.Test
{
	[TestClass]
	public class CryptoTransformingBufferedSourceTests
	{
		[TestMethod]
		[TestCategory ("BufferedSource")]
		public void CryptoTransformingBufferedSource_ReadAll ()
		{
			TransformAllTest (7283, 2911, 0, 1474, 11824, true, 8007);
			TransformAllTest (7283, 2911, 2, 1474, 11824, true, 8007);
			TransformAllTest (7283, 2911, 0, 1474, 11824, false, 8007);
			TransformAllTest (7283, 2911, 2, 1474, 11824, false, 8007);
			TransformAllTest (3106, 9476, 0, 7199, 10486, true, 19051);
			TransformAllTest (3106, 9476, 3, 7199, 10486, true, 19051);
			TransformAllTest (3106, 9476, 0, 7199, 10486, false, 19051);
			TransformAllTest (3106, 9476, 3, 7199, 10486, false, 19051);

			TransformAllTest (2911, 2911, 0, 1474, 11824, true, 8007);
			TransformAllTest (2911, 2911, 2, 1474, 11824, true, 8007);
			TransformAllTest (2911, 2911, 0, 1474, 11824, false, 8007);
			TransformAllTest (2911, 2911, 2, 1474, 11824, false, 8007);
			TransformAllTest (9476, 9476, 0, 7199, 10486, true, 19051);
			TransformAllTest (9476, 9476, 3, 7199, 10486, true, 19051);
			TransformAllTest (9476, 9476, 0, 7199, 10486, false, 19051);
			TransformAllTest (9476, 9476, 3, 7199, 10486, false, 19051);

			TransformAllTest (1, 211, 0, 144, 3, true, 8007);
			TransformAllTest (1, 211, 2, 144, 3, true, 8007);
			TransformAllTest (1, 211, 0, 144, 3, false, 8007);
			TransformAllTest (1, 211, 2, 144, 3, false, 8007);
			TransformAllTest (1, 76, 0, 71, 127, true, 19051);
			TransformAllTest (1, 76, 3, 71, 127, true, 19051);
			TransformAllTest (1, 76, 0, 71, 127, false, 19051);
			TransformAllTest (1, 76, 3, 71, 127, false, 19051);

			TransformAllTest (211, 1, 0, 144, 11824, true, 3);
			TransformAllTest (211, 1, 2, 144, 11824, true, 3);
			TransformAllTest (211, 1, 0, 144, 11824, false, 3);
			TransformAllTest (211, 1, 2, 144, 11824, false, 3);
			TransformAllTest (76, 1, 0, 71, 10486, true, 127);
			TransformAllTest (76, 1, 3, 71, 10486, true, 127);
			TransformAllTest (76, 1, 0, 71, 10486, false, 127);
			TransformAllTest (76, 1, 3, 71, 10486, false, 127);

			TransformAllTest (1, 1, 0, 144, 11824, true, 3);
			TransformAllTest (1, 1, 2, 144, 11824, true, 3);
			TransformAllTest (1, 1, 0, 144, 11824, false, 3);
			TransformAllTest (1, 1, 2, 144, 11824, false, 3);
			TransformAllTest (1, 1, 0, 71, 10486, true, 127);
			TransformAllTest (1, 1, 3, 71, 10486, true, 127);
			TransformAllTest (1, 1, 0, 71, 10486, false, 127);
			TransformAllTest (1, 1, 3, 71, 10486, false, 127);

			SkipTransformChunkTest (5142, 3191, 0, 2334806618776893638L, 8261, true, 11634, 1205L, 1594);
			SkipTransformChunkTest (5142, 3191, 5, 2334806618776893638L, 8261, true, 11634, 1205L, 1594);
			SkipTransformChunkTest (5142, 3191, 0, 2334806618776893638L, 8261, false, 11634, 1205L, 1594);
			SkipTransformChunkTest (5142, 3191, 5, 2334806618776893638L, 8261, false, 11634, 1205L, 1594);
			SkipTransformChunkTest (121, 3191, 0, 1728006203025082346L, 17580, true, 4003, 12050L, 1);
			SkipTransformChunkTest (121, 3191, 1, 1728006203025082346L, 17580, true, 4003, 12050L, 1);
			SkipTransformChunkTest (121, 3191, 0, 1728006203025082346L, 17580, false, 4003, 12050L, 1);
			SkipTransformChunkTest (121, 3191, 1, 1728006203025082346L, 17580, false, 4003, 12050L, 1);

			SkipTransformChunkTest (3191, 3191, 0, 3168L, 8261, true, 11634, 1205L, 1594);
			SkipTransformChunkTest (3191, 3191, 5, 3168L, 8261, true, 11634, 1205L, 1594);
			SkipTransformChunkTest (3191, 3191, 0, 3168L, 8261, false, 11634, 1205L, 1594);
			SkipTransformChunkTest (3191, 3191, 5, 3168L, 8261, false, 11634, 1205L, 1594);
			SkipTransformChunkTest (3191, 3191, 0, 757372522887491490L, 17580, true, 4003, 12050L, 1);
			SkipTransformChunkTest (3191, 3191, 1, 757372522887491490L, 17580, true, 4003, 12050L, 1);
			SkipTransformChunkTest (3191, 3191, 0, 757372522887491490L, 17580, false, 4003, 12050L, 1);
			SkipTransformChunkTest (3191, 3191, 1, 757372522887491490L, 17580, false, 4003, 12050L, 1);

			SkipTransformChunkTest (1, 3191, 0, int.MaxValue, 6, true, 11634, 1205L, 1594);
			SkipTransformChunkTest (1, 3191, 5, int.MaxValue, 6, true, 11634, 1205L, 1594);
			SkipTransformChunkTest (1, 3191, 0, int.MaxValue, 6, false, 11634, 1205L, 1594);
			SkipTransformChunkTest (1, 3191, 5, int.MaxValue, 6, false, 11634, 1205L, 1594);
			SkipTransformChunkTest (1, 3191, 0, 151852718393714646L, 256, true, 4003, 12050L, 1);
			SkipTransformChunkTest (1, 3191, 1, 151852718393714646L, 256, true, 4003, 12050L, 1);
			SkipTransformChunkTest (1, 3191, 0, 151852718393714646L, 256, false, 4003, 12050L, 1);
			SkipTransformChunkTest (1, 3191, 1, 151852718393714646L, 256, false, 4003, 12050L, 1);

			SkipTransformChunkTest (191, 1, 0, 2101L, 8261, true, 6, 5L, 0);
			SkipTransformChunkTest (191, 1, 5, 2101L, 8261, true, 6, 5L, 0);
			SkipTransformChunkTest (191, 1, 0, 2101L, 8261, false, 6, 5L, 0);
			SkipTransformChunkTest (191, 1, 5, 2101L, 8261, false, 6, 5L, 0);
			SkipTransformChunkTest (191, 1, 0, 1925178341944135524L, 17580, true, 256, 12050L, 1);
			SkipTransformChunkTest (191, 1, 1, 1925178341944135524L, 17580, true, 256, 12050L, 1);
			SkipTransformChunkTest (191, 1, 0, 1925178341944135524L, 17580, false, 256, 12050L, 1);
			SkipTransformChunkTest (191, 1, 1, 1925178341944135524L, 17580, false, 256, 12050L, 1);

			SkipTransformChunkTest (1, 1, 0, 11L, 8261, true, 6, 5L, 0);
			SkipTransformChunkTest (1, 1, 5, 11L, 8261, true, 6, 5L, 0);
			SkipTransformChunkTest (1, 1, 0, 11L, 8261, false, 6, 5L, 0);
			SkipTransformChunkTest (1, 1, 5, 11L, 8261, false, 6, 5L, 0);
			SkipTransformChunkTest (1, 1, 0, 1925178341944135524L, 17580, true, 256, 12050L, 1);
			SkipTransformChunkTest (1, 1, 1, 1925178341944135524L, 17580, true, 256, 12050L, 1);
			SkipTransformChunkTest (1, 1, 0, 1925178341944135524L, 17580, false, 256, 12050L, 1);
			SkipTransformChunkTest (1, 1, 1, 1925178341944135524L, 17580, false, 256, 12050L, 1);
		}
		private static void SkipTransformChunkTest (
			int inBlockSize,
			int outBlockSize,
			int inputCacheBlocks,
			long dataSize,
			int bufSize,
			bool canTransformMultipleBlocks,
			int transformBufferSize,
			long totalSkip,
			int bufferSkip)
		{
			var src = new BigBufferedSourceMock (dataSize, bufSize, FillFunction);
			var mock = new CryptoTransformMock (inBlockSize, outBlockSize, inputCacheBlocks, canTransformMultipleBlocks);
			var transform = new CryptoTransformingBufferedSource (src, mock, new byte[transformBufferSize]);

			Assert.AreEqual (totalSkip, transform.TrySkip (totalSkip));
			transform.EnsureBuffer (3);
			Assert.AreEqual ((byte)~FillFunction (MapTransformIndexBackToOriginal (totalSkip, mock)), transform.Buffer[transform.Offset]);
			Assert.AreEqual ((byte)~FillFunction (MapTransformIndexBackToOriginal (totalSkip + 1, mock)), transform.Buffer[transform.Offset + 1]);
			Assert.AreEqual ((byte)~FillFunction (MapTransformIndexBackToOriginal (totalSkip + 2, mock)), transform.Buffer[transform.Offset + 2]);
			transform.EnsureBuffer (bufferSkip);
			transform.SkipBuffer (bufferSkip);
			transform.EnsureBuffer (3);
			Assert.AreEqual ((byte)~FillFunction (MapTransformIndexBackToOriginal (totalSkip + bufferSkip, mock)), transform.Buffer[transform.Offset]);
			Assert.AreEqual ((byte)~FillFunction (MapTransformIndexBackToOriginal (totalSkip + bufferSkip + 1, mock)), transform.Buffer[transform.Offset + 1]);
			Assert.AreEqual ((byte)~FillFunction (MapTransformIndexBackToOriginal (totalSkip + bufferSkip + 2, mock)), transform.Buffer[transform.Offset + 2]);
		}
		private static long MapTransformIndexBackToOriginal (long resultIndex, ICryptographicTransform transform)
		{
			var blockNumber = resultIndex / (long)transform.OutputBlockSize;
			var blockOffset = ((resultIndex % (long)transform.OutputBlockSize) % (long)transform.InputBlockSize);
			return blockNumber * transform.InputBlockSize + blockOffset;
		}
		private static void TransformAllTest (
			int inBlockSize,
			int outBlockSize,
			int inputCacheBlocks,
			int dataSize,
			int bufSize,
			bool canTransformMultipleBlocks,
			int transformBufferSize)
		{
			var src = new BigBufferedSourceMock (dataSize, bufSize, FillFunction);
			var mock = new CryptoTransformMock (inBlockSize, outBlockSize, inputCacheBlocks, canTransformMultipleBlocks);
			var transform = new CryptoTransformingBufferedSource (src, mock, new byte[transformBufferSize]);

			var fullBlocks = dataSize / inBlockSize;
			var inReminder = dataSize - fullBlocks * inBlockSize;
			var outReminder = Math.Min (inReminder, outBlockSize);
			var resultSize = fullBlocks * outBlockSize + outReminder;

			// считываем весь результат преобразования
			int len = 0;
			var result = new byte[resultSize];
			int available;
			while ((available = transform.FillBuffer ()) > 0)
			{
				Array.Copy (transform.Buffer, transform.Offset, result, len, available);
				len += available;
				transform.SkipBuffer (available);
			}
			Assert.IsTrue (transform.IsExhausted);
			Assert.AreEqual (resultSize, len);

			// проверяем что результат преобразования совпадает с тестовым массивом
			for (var i = 0; i < fullBlocks; i++)
			{
				for (int j = 0; j < outBlockSize; j++)
				{
					var inIndex = i * inBlockSize + (j % inBlockSize);
					var outIndex = i * outBlockSize + j;
					Assert.AreEqual (FillFunction (inIndex), (byte)~result[outIndex]);
				}
			}
			for (int j = 0; j < outReminder; j++)
			{
				var inIndex = fullBlocks * inBlockSize + (j % inBlockSize);
				var outIndex = fullBlocks * outBlockSize + j;
				Assert.AreEqual (FillFunction (inIndex), (byte)~result[outIndex]);
			}
		}
		private static byte FillFunction (long position)
		{
			return (byte)(0xAA ^ (position & 0xFF));
		}
	}
}
