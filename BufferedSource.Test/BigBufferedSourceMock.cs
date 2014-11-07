using System;

namespace BusinessClassLibrary.Test
{
	// имитирует источник данных большого размера без выделения ресурсов
	// заполняет буфер путем вызова указанной функции, в которую передается абсолютная позиция в источнике
	public class BigBufferedSourceMock :
		IBufferedSource
	{
		private readonly byte[] _buffer;
		private readonly long _size = 0L;
		private readonly Func<long, byte> _dataFunction;
		private long _position = 0;
		private int _offset = 0;
		private int _count = 0;
		private bool _isExhausted = false;

		public byte[] Buffer { get { return _buffer; } }

		public int Offset { get { return _offset; } }

		public int Count { get { return _count; } }

		public bool IsExhausted { get { return _isExhausted; } }

		public BigBufferedSourceMock (long size, int bufferSize, Func<long, byte> dataFunction)
		{
			if (bufferSize < 1)
			{
				throw new ArgumentOutOfRangeException ("bufferSize");
			}

			_size = size;
			_buffer = new byte[bufferSize];
			_dataFunction = dataFunction;
		}

		public void SkipBuffer (int size)
		{
			if ((size < 0) || (size > this.Count))
			{
				throw new ArgumentOutOfRangeException ("size");
			}

			if (size > 0)
			{
				_offset += size;
				_count -= size;
			}
		}

		public int FillBuffer ()
		{
			if (!_isExhausted)
			{
				if (_offset > 0)
				{
					// сдвигаем в начало данные буфера
					if (_count > 0)
					{
						Array.Copy (_buffer, _offset, _buffer, 0, _count);
					}
					_offset = 0;
				}

				int i = 0;
				while (((_offset + _count + i) < _buffer.Length) && ((_position + i) < _size))
				{
					_buffer[_offset + _count + i] = _dataFunction.Invoke (_position + i);
					i++;
				}
				if ((_position + i) >= _size)
				{
					_isExhausted = true;
				}
				_count += i;
				_position += i;
			}
			return _count;
		}

		public void EnsureBuffer (int size)
		{
			if ((size < 0) || (size > this.Buffer.Length))
			{
				throw new ArgumentOutOfRangeException ("size");
			}

			if (size > 0)
			{
				var available = _count;
				var shortage = size - available;
				// данных в буфере достаточно или запрашивать их безполезно
				if ((shortage > 0) && !_isExhausted)
				{
					// сдвигаем в начало данные буфера
					if (_offset > 0)
					{
						if (available > 0)
						{
							Array.Copy (_buffer, _offset, _buffer, 0, available);
						}
						_offset = 0;
					}

					int i = 0;
					while ((i < shortage) && ((_position + i) < _size))
					{
						_buffer[_offset + _count + i] = _dataFunction.Invoke (_position + i);
						i++;
					}
					if ((_position + i) >= _size)
					{
						_isExhausted = true;
					}
					_count += i;
					_position += i;
				}
				if (shortage > 0)
				{
					throw new InvalidOperationException ("Source exhausted and can not provide requested size of data.");
				}
			}
		}

		public long TrySkip (long size)
		{
			if (size < 0L)
			{
				throw new ArgumentOutOfRangeException ("size");
			}

			long skipped = 0L;
			var availableBuffer = _count;
			if (size <= (long)availableBuffer)
			{
				_offset += (int)size;
				return size;
			}
			if (availableBuffer > 0)
			{
				size -= availableBuffer;
				skipped += availableBuffer;
				_count = 0;
			}
			var availableSource = _size - _position;
			if (size > availableSource)
			{
				_position = _size;
				skipped += availableSource;
			}
			else
			{
				_position += size;
				skipped += size;
			}
			return skipped;
		}
	}
}
