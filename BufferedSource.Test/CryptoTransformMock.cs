using System;
using ICryptographicTransform = System.Security.Cryptography.ICryptoTransform;

namespace BusinessClassLibrary.Test
{
	/// <summary>
	/// Крипто-преобразование, которое напрямую копирует входные данные в выходные с инвертированием каждого байта,
	/// используя указанные размеры входного и выходного блока,
	/// а также задерживающее (кэширующее) указанное количество входных блоков.
	/// В случае если выходной блок меньше входного, то данные каждого входного блока обрезаются.
	/// В случае если выходной блок больше входного, то данные каждого входного блока повторяются.
	/// </summary>
	internal class CryptoTransformMock : ICryptographicTransform
	{
		private readonly bool _canTransformMultipleBlocks;
		private readonly int _inputBlockSize;
		private readonly int _outputBlockSize;
		private readonly int _cacheBlocksLimit;
		private readonly byte[] _cache;
		private int _cachedBlocks = 0;

		public CryptoTransformMock (int inputBlockSize, int outputBlockSize, int inputCacheBlocks, bool canTransformMultipleBlocks)
		{
			if (inputBlockSize < 1) throw new ArgumentOutOfRangeException ("inputBlockSize");
			if (outputBlockSize < 1) throw new ArgumentOutOfRangeException ("inputBlockSize");
			if (inputCacheBlocks < 0) throw new ArgumentOutOfRangeException ("inputCacheBlocks");

			_inputBlockSize = inputBlockSize;
			_outputBlockSize = outputBlockSize;
			_cacheBlocksLimit = inputCacheBlocks;
			_cache = new byte[_cacheBlocksLimit * inputBlockSize];
			_canTransformMultipleBlocks = canTransformMultipleBlocks;
		}

		public bool CanReuseTransform { get { return true; } }

		public bool CanTransformMultipleBlocks { get { return _canTransformMultipleBlocks; } }

		public int InputBlockSize { get { return _inputBlockSize; } }

		public int OutputBlockSize { get { return _outputBlockSize; } }

		/// <summary>
		/// Ничего не делает.
		/// </summary>
		public void Dispose () { }

		public int TransformBlock (byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
		{
			if (inputBuffer == null)
			{
				throw new ArgumentNullException ("inputBuffer");
			}
			if ((inputOffset < 0) || (inputOffset > inputBuffer.Length) || ((inputOffset == inputBuffer.Length) && (inputCount > 0)))
			{
				throw new ArgumentOutOfRangeException ("inputOffset");
			}
			if ((inputCount < 0) || ((inputOffset + inputCount) > inputBuffer.Length))
			{
				throw new ArgumentOutOfRangeException ("inputCount");
			}
			if (outputBuffer == null)
			{
				throw new ArgumentNullException ("outputBuffer");
			}
			if ((outputOffset < 0) || (outputOffset > outputBuffer.Length) || ((outputOffset == outputBuffer.Length) && (inputCount > 0)))
			{
				throw new ArgumentOutOfRangeException ("outputOffset");
			}
			if (!_canTransformMultipleBlocks && (inputCount > _inputBlockSize))
			{
				throw new ArgumentOutOfRangeException ("inputCount");
			}

			//Trace.WriteLine (string.Format ("---TransformBlock: inputBuffer.Length = {0}, inputOffset = {1}, inputCount = {2}, outputBuffer.Length = {3}, outputOffset = {4}, cachedBlocks = {5}", inputBuffer.Length, inputOffset, inputCount, outputBuffer.Length, outputOffset, _cachedBlocks));
			if ((inputCount % _inputBlockSize) != 0)
			{
				throw new InvalidOperationException ("inputCount must be multiple of InputBlockSize");
			}
			var inputBlocks = inputCount / _inputBlockSize;
			var outputBlocks = inputBlocks;
			if ((inputBlocks * _outputBlockSize) > (outputBuffer.Length - outputOffset))
			{
				throw new InvalidOperationException ("available size of outputBuffer must hold all the blocks specified in inputCount");
			}

			int result;
			// все влезет в кэш, на выход - ничего
			if ((_cachedBlocks + inputBlocks) <= _cacheBlocksLimit)
			{
				Array.Copy (inputBuffer, inputOffset, _cache, _cachedBlocks * _inputBlockSize, inputCount);
				_cachedBlocks += inputBlocks;
				result = 0;
			}
			else
			{
				// заполняем начало выхода тем что в кэше
				if (_cachedBlocks > 0)
				{
					var blocksFromCache = Math.Min (_cachedBlocks, inputBlocks);
					// преобразуем часть кэша в выход
					Transform (_cache, 0, outputBuffer, outputOffset, blocksFromCache);
					// сдвигаем остаток кэша в начало
					Array.Copy (_cache, blocksFromCache * _inputBlockSize, _cache, 0, (_cachedBlocks - blocksFromCache) * _inputBlockSize);
					_cachedBlocks -= blocksFromCache;
					outputOffset += blocksFromCache * _outputBlockSize;
					outputBlocks -= blocksFromCache;
				}

				// заполняем оставшийся выход из входа
				Transform (inputBuffer, inputOffset, outputBuffer, outputOffset, outputBlocks);
				inputOffset += outputBlocks * _inputBlockSize;

				// остаток входа сохраняем в кэш
				Array.Copy (inputBuffer, inputOffset, _cache, _cachedBlocks * _inputBlockSize, (inputBlocks - outputBlocks) * _inputBlockSize);
				_cachedBlocks += inputBlocks - outputBlocks;
				result = inputBlocks * _outputBlockSize;
			}
			//Trace.WriteLine (string.Format ("---TransformBlock: result = {0}", result));
			return result;
		}
		private void Transform (byte[] input, int inputOffset, byte[] output, int outputOffset, int nBlocks)
		{
			for (int i = 0; i < nBlocks; i++)
			{
				for (int j = 0; j < _outputBlockSize; j++)
				{
					var inIndex = inputOffset + (i * _inputBlockSize) + (j % _inputBlockSize);
					var outIndex = outputOffset + (i * _outputBlockSize) + j;
					output[outIndex] = (byte)~input[inIndex];
				}
			}
		}

		public byte[] TransformFinalBlock (byte[] inputBuffer, int inputOffset, int inputCount)
		{
			//Trace.WriteLine (string.Format ("---TransformFinalBlock: inputBuffer.Length = {0}, inputOffset = {1}, inputCount = {2}, cachedBlocks = {3}", inputBuffer.Length, inputOffset, inputCount, _cachedBlocks));
			var inputBlocks = inputCount / _inputBlockSize;
			var inputReminder = inputCount - inputBlocks * _inputBlockSize;
			var outputReminder = Math.Min (inputReminder, _outputBlockSize);
			var result = new byte[(_cachedBlocks + inputBlocks) * _outputBlockSize + outputReminder];
			// данные из кэша
			Transform (_cache, 0, result, 0, _cachedBlocks);
			var outputOffset = _cachedBlocks * _outputBlockSize;
			// данные входа кратно блокам
			Transform (inputBuffer, inputOffset, result, outputOffset, inputBlocks);
			inputOffset += inputBlocks * _inputBlockSize;
			outputOffset += inputBlocks * _outputBlockSize;
			// оставшиеся данные
			for (int j = 0; j < outputReminder; j++)
			{
				result[outputOffset + j] = (byte)~inputBuffer[inputOffset + (j % inputReminder)];
			}
			//Trace.WriteLine (string.Format ("---TransformFinalBlock: result.Length = {0}", result.Length));
			return result;
		}
	}
}
