using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusinessClassLibrary.Test
{
	[TestClass]
	public class EvaluatorDelimitedBufferedSourceTests
	{
		// считает содержимым одной части байты >=100, при этом идущие следом байты <100 являются разделителем частей
		public class OneHundredEvaluatorBufferedSource : EvaluatorPartitionedBufferedSourceBase
		{
			private readonly IBufferedSource _source;
			private int _epilogueSize = -1;

			public OneHundredEvaluatorBufferedSource (IBufferedSource source)
				: base (source)
			{
				_source = source;
			}

			protected override bool IsEndOfPartFound { get { return _epilogueSize >= 0; } }

			protected override int PartEpilogueSize { get { return _epilogueSize; } }

			protected override int ValidatePartData (int validatedPartLength)
			{
				_epilogueSize = -1;
				var startOffset = _source.Offset + validatedPartLength;
				while (startOffset < (_source.Offset + _source.Count))
				{
					if (_source.Buffer[startOffset] < 100)
					{
						var epilogueSize = 1;
						while ((startOffset + epilogueSize) < (_source.Offset + _source.Count))
						{
							if (_source.Buffer[startOffset + epilogueSize] >= 100)
							{
								_epilogueSize = epilogueSize;
								break;
							}
							epilogueSize++;
						}
						if (_source.IsExhausted)
						{
							_epilogueSize = epilogueSize;
						}
						return startOffset - _source.Offset;
					}
					startOffset++;
				}
				return _source.Count;
			}
		}

		[TestMethod]
		[TestCategory ("BufferedSource")]
		public void EvaluatorPartitionedBufferedSourceBase_ReadAndSkipPart ()
		{
			long skipBeforeLimitingSize = 0x408c5f081052008cL;

			long firstPartPos = 0x408c5f08105200c0L;
			long secondPartPos = 0x408c5f08105200ccL;
			long thirdPartPos = 0x408c5f0810520100L;
			int srcBufSize = 32768;

			// части в середине источника
			var subSrc = new BigBufferedSourceMock (long.MaxValue, srcBufSize, FillFunction);
			subSrc.TrySkip (skipBeforeLimitingSize);
			var src = new OneHundredEvaluatorBufferedSource (subSrc);
			Assert.AreEqual (0, src.FillBuffer ());
			Assert.IsTrue (src.TrySkipPart ());
			src.EnsureBuffer (3);
			Assert.AreEqual (FillFunction (firstPartPos), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction (firstPartPos + 1), src.Buffer[src.Offset + 1]);
			Assert.AreEqual (FillFunction (firstPartPos + 2), src.Buffer[src.Offset + 2]);
			Assert.IsTrue (src.TrySkipPart ());
			src.EnsureBuffer (3);
			Assert.AreEqual (FillFunction (secondPartPos), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction (secondPartPos + 1), src.Buffer[src.Offset + 1]);
			Assert.AreEqual (FillFunction (secondPartPos + 2), src.Buffer[src.Offset + 2]);
			Assert.IsTrue (src.TrySkipPart ());
			src.EnsureBuffer (3);
			Assert.AreEqual (FillFunction (thirdPartPos), src.Buffer[src.Offset]);
			Assert.AreEqual (FillFunction (thirdPartPos + 1), src.Buffer[src.Offset + 1]);
			Assert.AreEqual (FillFunction (thirdPartPos + 2), src.Buffer[src.Offset + 2]);

			// части в конце источника
			long size = 0x4000000000000000L;
			subSrc = new BigBufferedSourceMock (size, srcBufSize, FillFunction);
			subSrc.TrySkip (0x3fffffffffffffc0L); // отступаем так чтобы осталось две части с хвостиком
			src = new OneHundredEvaluatorBufferedSource (subSrc);
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
