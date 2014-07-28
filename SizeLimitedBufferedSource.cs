using System;
using System.Diagnostics.Contracts;

namespace BusinessClassLibrary
{
	/// <summary>
	/// Источник данных, представленный байтовым буфером,
	/// предоставляющий указанное число байтов из другого источника данных.
	/// </summary>
	[System.Diagnostics.DebuggerDisplay ("{Offset}...{Offset+Count} ({Buffer.Length}) exhausted={IsExhausted}")]
	public class SizeLimitedBufferedSource :
		IBufferedSource
	{
		private readonly IBufferedSource _source;
		private int _countInBuffer;
		private long _countRemainder;

		/// <summary>
		/// Остаток лимита в текущем буфере.
		/// </summary>
		protected int CurrentSize { get { return _countInBuffer; } }

		/// <summary>
		/// Остаток лимита, который не исчерпан в текущем буфере,
		/// либо -1 если лимит исчерпан в текущем буфере.
		/// </summary>
		protected long RemainingSize { get { return _countRemainder; } }

		/// <summary>
		/// Инициализирует новый экземпляр SizeLimitedBufferedSource получающий данные из указанного IBufferedSource
		/// разделяя его на порции указанного размера.
		/// </summary>
		/// <param name="source">Источник данных, представляющий из себя порции фиксированного размера.</param>
		/// <param name="limit">Размер порции данных.</param>
		public SizeLimitedBufferedSource (IBufferedSource source, long limit)
		{
			if (source == null)
			{
				throw new ArgumentNullException ("source");
			}
			if (limit < 0L)
			{
				throw new ArgumentOutOfRangeException ("limit");
			}
			Contract.EndContractBlock ();

			_source = source;

			UpdateLimits (limit);
		}

		/// <summary>
		/// Получает буфер, в котором содержится некоторая часть данных источника.
		/// Текущая начальная позиция и количество доступных данных содержатся в свойствах Offset и Count,
		/// при этом сам буфер остаётся неизменным всё время жизни источника.
		/// </summary>
		public byte[] Buffer { get { return _source.Buffer; } }

		/// <summary>
		/// Получает начальную позицию данных, доступных в Buffer.
		/// Количество данных, доступных в Buffer, содержится в Count.
		/// </summary>
		public int Offset { get { return _source.Offset; } }

		/// <summary>
		/// Получает количество данных, доступных в Buffer.
		/// Начальная позиция доступных данных содержится в Offset.
		/// </summary>
		public int Count { get { return _countInBuffer; } }

		/// <summary>Получает признак исчерпания источника.
		/// Возвращает True если источник больше не поставляет данных.
		/// Содержимое буфера при этом остаётся верным, но больше не будет меняться.</summary>
		public bool IsExhausted { get { return (_source.IsExhausted || (_countRemainder <= 0L)); } }

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
				_source.SkipBuffer (size);
				_countInBuffer -= size;
			}
		}

		/// <summary>
		/// Осуществляет попытку пропустить указанное количество данных источника, включая уже доступные в буфере данные.
		/// При выполнении могут измениться свойства Offset, Count и IsExhausted.
		/// </summary>
		/// <param name="size">Размер данных для пропуска.</param>
		/// <returns>
		/// Количество пропущеных байтов данных, включая уже доступные в буфере данные.
		/// Может быть меньше, чем было указано, если источник исчерпался.
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

			var limit = (long)_countInBuffer + _countRemainder;
			long skipped = 0L;
			if (size < limit)
			{
				// пропуск в пределах лимита
				skipped = _source.TrySkip (size);
				if (skipped >= size)
				{
					UpdateLimits (limit - skipped);
					return skipped;
				}
			}
			else
			{
				// пропуск больше лимита
				if (limit > 0L)
				{
					skipped = _source.TrySkip (limit);
				}
			}
			// не удалось пропустить сколько надо, делаем наш источник пустым
			_countInBuffer = 0;
			_countRemainder = 0L;
			return skipped;
		}

		/// <summary>
		/// Заполняет буфер данными источника, дополняя уже доступные там данные.
		/// В результате буфер может быть заполнен неполностью если источник поставляет данные блоками, либо пуст если источник исчерпался.
		/// При выполнении могут измениться свойства Offset, Count и IsExhausted.
		/// </summary>
		/// <returns>Размер доступных в буфере данных. Если ноль, то источник исчерпан и доступных данных в буфере больше не будет.</returns>
		public int FillBuffer ()
		{
			if (_countRemainder > 0L)
			{
				_source.FillBuffer ();
				UpdateLimits ((long)_countInBuffer + _countRemainder);
			}
			return _countInBuffer;
		}

		/// <summary>
		/// Запрашивает у источника указанное количество данных в буфере.
		/// В результате запроса в буфере может оказаться данных больше, чем запрошено.
		/// При выполнении могут измениться свойства Offset, Count и IsExhausted.
		/// </summary>
		/// <param name="size">Требуемый размер данных в буфере.</param>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// Происходит если size меньше единицы или больше размера буфера.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Происходит если источник не смог предоставить в буфере указанное количество данных.
		public void EnsureBuffer (int size)
		{
			if ((size < 0) || (size > this.Buffer.Length))
			{
				throw new ArgumentOutOfRangeException ("size");
			}
			Contract.EndContractBlock ();

			if (size > 0)
			{
				while ((size > _countInBuffer) && !_source.IsExhausted)
				{
					_source.FillBuffer ();
					UpdateLimits ((long)_countInBuffer + _countRemainder);
				}
				if (size > _countInBuffer)
				{
					throw new InvalidOperationException ("Source exhausted and can not provide requested size of data.");
				}
			}
		}

		/// <summary>
		/// Обновляет границы данных.
		/// </summary>
		protected void UpdateLimits (long limit)
		{
			_countRemainder = limit - (long)_source.Count;

			if (_countRemainder > 0)
			{
				_countInBuffer = _source.Count;
			}
			else
			{
				_countRemainder = 0L;
				_countInBuffer = (int)limit;
			}
		}
	}
}
