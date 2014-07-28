using System;
using System.Diagnostics.Contracts;

namespace BusinessClassLibrary
{
	/// <summary>
	/// Базовый класс - источник данных, представленный байтовым буфером,
	/// предоставляющий данные другого источника данных,
	/// разделяя их по результатам вызова метода.
	/// </summary>
	[System.Diagnostics.DebuggerDisplay ("{Offset}...{Offset+Count} ({Buffer.Length}) exhausted={IsExhausted}")]
	public abstract class EvaluatorPartitionedBufferedSourceBase :
		IPartitionedBufferedSource
	{
		private readonly IBufferedSource _source;
		private int _partValidatedLength = 0;

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
		public int Count { get { return _partValidatedLength; } }

		/// <summary>Получает признак исчерпания одной части источника.
		/// Возвращает True если источник больше не поставляет данных части.
		/// Содержимое буфера при этом остаётся верным, но больше не будет меняться.</summary>
		public bool IsExhausted
		{
			get
			{
				return this.IsEndOfPartFound ||
					(_source.IsExhausted && (_partValidatedLength >= _source.Count)); // проверен весь остаток источника
			}
		}

		/// <summary>
		/// Возвращает размер данных в буфере, которые принадлежат одной части.
		/// </summary>
		protected int PartValidatedLength { get { return _partValidatedLength; } }

		/// <summary>
		/// Возвращает признак того, что в буфере содержится конец части.
		/// </summary>
		protected abstract bool IsEndOfPartFound { get; }

		/// <summary>
		/// Возвращает размер эпилога части,
		/// то есть куска который будет пропущен при переходе на следующую часть.
		/// </summary>
		protected abstract int PartEpilogueSize { get; }

		/// <summary>
		/// Проверяет данные на принадлежность к одной части.
		/// Также обновляет свойства IsEndOfPartFound и PartEpilogueSize.
		/// </summary>
		/// <param name="validatedPartLength">Размер уже проверенных данных.</param>
		/// <returns>Размер данных в буфере, которые принадлежат одной части.</returns>
		protected abstract int ValidatePartData (int validatedPartLength);

		/// <summary>
		/// Инициализирует новый экземпляр EvaluatorPartitionedBufferedSourceBase получающий данные из указанного IBufferedSource
		/// разделяя его по результатам вызова указанной функции.
		/// </summary>
		/// <param name="source">Источник данных.</param>
		protected EvaluatorPartitionedBufferedSourceBase (IBufferedSource source)
		{
			if (source == null)
			{
				throw new ArgumentNullException ("source");
			}
			Contract.EndContractBlock ();

			_source = source;
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
				_source.SkipBuffer (size);
				_partValidatedLength -= size;
			}
		}


		/// <summary>
		/// Осуществляет попытку пропустить указанное количество данных источника, включая уже доступные в буфере данные.
		/// При выполнении могут измениться свойства Offset, Count и IsExhausted.
		/// </summary>
		/// <param name="size">Количество байтов данных для пропуска.</param>
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

			long skipped = 0L;
			while ((long)_partValidatedLength < size)
			{
				_source.SkipBuffer (_partValidatedLength);
				size -= (long)_partValidatedLength;
				skipped += (long)_partValidatedLength;
				if (this.IsEndOfPartFound)
				{
					return skipped;
				}
				_source.FillBuffer ();
				// если изменилась позиция данных в буфере то сами данные, доступные ранее, не могут измениться, могут лишь добавиться новые
				_partValidatedLength = ValidatePartData (_partValidatedLength);
			}
			_source.SkipBuffer ((int)size);
			skipped += size;

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
			if (!this.IsEndOfPartFound)
			{
				_source.FillBuffer ();
				_partValidatedLength = ValidatePartData (_partValidatedLength);
			}
			return _partValidatedLength;
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
				while ((size > _partValidatedLength) && !_source.IsExhausted)
				{
					_source.FillBuffer ();
					_partValidatedLength = ValidatePartData (_partValidatedLength);
				}
				if (size > _partValidatedLength)
				{
					throw new InvalidOperationException ("Source exhausted and can not provide requested size of data.");
				}
			}
		}

		/// <summary>
		/// Пытается пропустить все данные источника, принадлежащие текущей части,
		/// чтобы стали доступны данные следующей части.
		/// </summary>
		/// <returns>
		/// True если источник переключился на следующую часть,
		/// либо False если источник исчерпался.
		/// </returns>
		public bool TrySkipPart ()
		{
			if (_source.IsExhausted && (_source.Count < 1))
			{
				// источник пуст
				return false;
			}

			// необходимо найти конец части если еще не найден
			while (!this.IsEndOfPartFound)
			{
				// пропускаем проверенные данные
				if (_partValidatedLength > 0)
				{
					SkipBuffer (_partValidatedLength);
				}
				if ((FillBuffer () <= 0) && !this.IsEndOfPartFound)
				{
					// в полном буфере не найдено ни подходящих данных, ни полного разделителя/эпилога
					// означает что разделитель не вместился в буфер
					throw new InvalidOperationException ("Buffer insufficient for detecting end of part.");
				}
			}

			SkipFoundPart ();

			_partValidatedLength = ValidatePartData (_partValidatedLength);

			return true;
		}

		/// <summary>
		/// Пропускает разделитель (и всё до него) когда он найден.
		/// </summary>
		protected void SkipFoundPart ()
		{
			// jump to found separator
			var sizeToSkip = _partValidatedLength + this.PartEpilogueSize;
			if (sizeToSkip > 0)
			{
				_source.SkipBuffer (sizeToSkip);
				_partValidatedLength = 0;
			}
		}

		protected void ValidatePartData ()
		{
			_partValidatedLength = ValidatePartData (_partValidatedLength);
		}
	}
}
