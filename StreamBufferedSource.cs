using System;
using System.IO;
using System.Diagnostics.Contracts;

namespace BusinessClassLibrary.IO
{
	/// <summary>
	/// Источник данных, представленный байтовым буфером,
	/// предоставляющий данные указанного потока.
	/// </summary>
	[System.Diagnostics.DebuggerDisplay ("{Offset}...{Offset+Count} ({Buffer.Length}) exhausted={IsExhausted}")]
	public class StreamBufferedSource :
		IBufferedSource
	{
		private readonly Stream _stream;
		private readonly byte[] _buffer;
		private int _offset = 0;
		private int _count = 0;
		private bool _streamEnded = false;

		/// <summary>
		/// Получает буфер, в котором содержится некоторая часть данных потока.
		/// Текущая начальная позиция и количество доступных данных содержатся в свойствах Offset и Count,
		/// при этом сам буфер остаётся неизменным всё время жизни источника.
		/// </summary>
		public byte[] Buffer { get { return _buffer; } }

		/// <summary>
		/// Получает начальную позицию данных, доступных в Buffer.
		/// Количество данных, доступных в Buffer, содержится в Count.
		/// </summary>
		public int Offset { get { return _offset; } }

		/// <summary>
		/// Получает количество данных, доступных в Buffer.
		/// Начальная позиция доступных данных содержится в Offset.
		/// </summary>
		public int Count { get { return _count; } }

		/// <summary>Получает признак исчерпания потока.
		/// Возвращает True если потока больше не поставляет данных.
		/// Содержимое буфера при этом остаётся верным, но больше не будет меняться.</summary>
		public bool IsExhausted { get { return _streamEnded; } }

		/// <summary>
		/// Инициализирует новый экземпляр StreamBufferedSource получающий данные из указанного потока
		/// используя буфер указанного размера.
		/// </summary>
		/// <param name="stream">Исходный поток для чтения данных.</param>
		/// <param name="buffer">Байтовый буфер, в котором будут содержаться считанные из потока данные.</param>
		public StreamBufferedSource (Stream stream, byte[] buffer)
		{
			if (stream == null)
			{
				throw new ArgumentNullException ("stream");
			}
			if (buffer == null)
			{
				throw new ArgumentNullException ("buffer");
			}
			if (!stream.CanRead)
			{
				throw new ArgumentOutOfRangeException ("stream");
			}
			if (buffer.Length < 1)
			{
				throw new ArgumentOutOfRangeException ("buffer");
			}
			Contract.EndContractBlock ();

			_stream = stream;
			_buffer = buffer;
		}

		/// <summary>Отбрасывает (пропускает) указанное количество данных из начала буфера.</summary>
		/// <param name="size">Размер данных для пропуска в начале буфера.
		/// Должен быть меньше чем размер данных в буфере.</param>
		public void SkipBuffer (int size)
		{
			if ((size < 0) || (size > this.Count))
			{
				throw new ArgumentOutOfRangeException ("size");
			}
			Contract.EndContractBlock ();

			if (size > 0)
			{
				_offset += size;
				_count -= size;
			}
		}
		/// <summary>
		/// Осуществляет попытку пропустить указанное количество данных потока, включая уже доступные в буфере данные.
		/// При выполнении могут измениться свойства Offset, Count и IsExhausted.
		/// </summary>
		/// <param name="size">Размер данных для пропуска.</param>
		/// <returns>
		/// Количество пропущеных байтов данных, включая уже доступные в буфере данные.
		/// Может быть меньше, чем было указано, если поток исчерпался.
		/// Источник будет предоставлять данные, идущие сразу за пропущенными.
		/// </returns>
		/// <exception cref="System.ArgumentOutOfRangeException">Происходит если size меньше нуля.</exception>
		public long TrySkip (long size)
		{
			if (size < 0L)
			{
				throw new ArgumentOutOfRangeException ("size");
			}
			Contract.EndContractBlock ();

			var available = _count;

			// достаточно доступных данных буфера
			if (size <= (long)available)
			{
				_offset += (int)size;
				_count -= (int)size;
				return size;
			}

			long skipped = available;
			// пропускаем весь буфер
			size -= (long)available;
			_offset = _count = 0;

			// поток поддерживает позиционирование, сразу переходим на нужную позицию
			if (_stream.CanSeek)
			{
				try
				{
					var streamAvailable = _stream.Length - _stream.Position;
					_streamEnded = (size >= streamAvailable);
					// если выходим за пределы размера потока то ставим указатель на конец
					if (size > streamAvailable)
					{
						_stream.Seek (0, SeekOrigin.End);
						skipped += streamAvailable;
						return skipped;
					}
					_stream.Seek (size, SeekOrigin.Current);
					skipped += size;
					return skipped;
				}
				// на случай если свойство Length, свойство Position или метод Seek() не поддерживаются потоком
				// будет использован метод последовательного чтения далее
				catch (NotSupportedException) { }
			}

			// поток не поддерживает позиционирование, читаем данные в буфер пока не считаем нужное количество
			int readed = 0;
			do
			{
				size -= (long)readed;
				skipped += (long)readed;
				readed = _stream.Read (_buffer, 0, _buffer.Length);
				if (readed < 1)
				{
					_streamEnded = true;
					return skipped;
				}
			} while (size > (long)readed);

			// делаем доступным остаток буфера
			skipped += size;
			_offset = (int)size;
			_count = readed - (int)size;
			return skipped;
		}

		/// <summary>
		/// Заполняет буфер данными потока, дополняя уже доступные там данные.
		/// В результате буфер может быть заполнен неполностью, либо пуст если поток исчерпался.
		/// При выполнении могут измениться свойства Offset, Count и IsExhausted.
		/// </summary>
		/// <returns>Размер доступных в буфере данных. Если ноль, то поток исчерпан и доступных данных в буфере больше не будет.</returns>
		public int FillBuffer ()
		{
			if (!_streamEnded && (_count < _buffer.Length))
			{
				Defragment ();

				var readed = _stream.Read (_buffer, _offset + _count, _buffer.Length - _offset - _count);
				_count += readed;
				if (readed < 1)
				{
					_streamEnded = true;
				}
			}
			return _count;
		}

		/// <summary>
		/// Запрашивает у потока указанное количество данных в буфере.
		/// В результате запроса в буфере может оказаться данных больше, чем запрошено.
		/// При выполнении могут измениться свойства Offset, Count и IsExhausted.
		/// </summary>
		/// <param name="size">Требуемый размер данных в буфере.</param>
		public void EnsureBuffer (int size)
		{
			if ((size < 0) || (size > this.Buffer.Length))
			{
				throw new ArgumentOutOfRangeException ("size");
			}
			Contract.EndContractBlock ();

			if (size > 0)
			{
				var available = _count;
				var shortage = size - available;
				if ((shortage > 0) && !_streamEnded)
				{
					Defragment ();

					// запускаем чтение потока пока не наберём необходимое количество данных
					while ((shortage > 0) && !_streamEnded)
					{
						var readed = _stream.Read (_buffer, _offset + _count, _buffer.Length - _offset - _count);
						shortage -= readed;
						_count += readed;
						if (readed < 1)
						{
							_streamEnded = true;
						}
					}
				}
				if (shortage > 0)
				{
					throw new InvalidOperationException ("Source exhausted and can not provide requested size of data.");
				}
			}
		}

		/// <summary>
		/// Принимает в буфер указанное количество считанных из потока данных.
		/// Добавленные данные должны располагаться в буфере начиная с позиции Count.
		/// </summary>
		/// <param name="count">Количество байтов, добавленных в буфер.</param>
		protected void AcceptChunk (int count)
		{
			_count += count;
		}

		/// <summary>
		/// Устанавливет признак исчерпания потока.
		/// </summary>
		protected void SetStreamEnded ()
		{
			_streamEnded = true;
		}

		/// <summary>
		/// Обеспечивает чтобы данные в буфере начинались с позиции ноль.
		/// </summary>
		protected void Defragment ()
		{
			// сдвигаем в начало данные буфера
			if (_offset > 0)
			{
				if (_count > 0)
				{
					Array.Copy (_buffer, _offset, _buffer, 0, _count);
				}
				_offset = 0;
			}
		}
	}
}
