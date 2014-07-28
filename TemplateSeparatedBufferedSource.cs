using System;
using System.Diagnostics.Contracts;

namespace BusinessClassLibrary
{
	/// <summary>
	/// Источник данных, представленный байтовым буфером,
	/// предоставляющий данные другого источника данных,
	/// разделяя их на части по указанному образцу-разделителю.
	/// </summary>
	[System.Diagnostics.DebuggerDisplay ("{Offset}...{Offset+Count} ({Buffer.Length}) exhausted={IsExhausted}")]
	public class TemplateSeparatedBufferedSource :
		IPartitionedBufferedSource
	{
		private readonly IBufferedSource _source;
		private readonly byte[] _template;
		private int _foundTemplateOffset;
		private int _foundTemplateLength = 0;

		/// <summary>
		/// Количество байтов в образце, являющимся разделитем источника данных.
		/// </summary>
		protected int SeparatorLength { get { return _template.Length; } }

		/// <summary>
		/// Позиция, в которой (возможно неполностью) найден образец.
		/// </summary>
		protected int FoundTemplateOffset { get { return _foundTemplateOffset; } }

		/// <summary>
		/// Размер части найденного образца.
		/// </summary>
		protected int FoundTemplateLength { get { return _foundTemplateLength; } }

		/// <summary>
		/// Инициализирует новый экземпляр TemplateSeparatedBufferedSource предоставляющий данные указанного источника данных,
		/// разделяя их на части по указанному образцу-разделителю.
		/// </summary>
		/// <param name="source">Источник данных, данные которого разделены на части указанным образцом-разделителем.</param>
		/// <param name="separator">Образец-разделитель, разделяющий источник на отдельные части.</param>
		public TemplateSeparatedBufferedSource (IBufferedSource source, byte[] separator)
		{
			if (source == null)
			{
				throw new ArgumentNullException ("source");
			}
			if (separator == null)
			{
				throw new ArgumentNullException ("separator");
			}
			if ((separator.Length < 1) || (separator.Length > source.Buffer.Length))
			{
				throw new ArgumentOutOfRangeException ("separator");
			}
			Contract.EndContractBlock ();

			_source = source;
			_template = separator;
			SearchBuffer (true);
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
		public int Count { get { return _foundTemplateOffset - _source.Offset; } }

		/// <summary>Получает признак исчерпания источника.
		/// Возвращает True если источник больше не поставляет данных.
		/// Содержимое буфера при этом остаётся верным, но больше не будет меняться.</summary>
		public bool IsExhausted { get { return (_source.IsExhausted || (_foundTemplateLength >= _template.Length)); } }

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

			int available;
			long skipped = 0L;
			while ((long)(available = _foundTemplateOffset - _source.Offset) < size)
			{
				_source.SkipBuffer (available);
				size -= (long)available;
				skipped += (long)available;
				if (_foundTemplateLength >= _template.Length)
				{
					return skipped;
				}
				var prevOffset = _source.Offset;
				_source.FillBuffer ();
				var resetSearch = (prevOffset != _source.Offset); // изменилась позиция данных в буфере. при этом сами данные, доступные ранее, не могут измениться, могут лишь добавиться новые
				SearchBuffer (resetSearch);
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
			if (_foundTemplateLength < _template.Length)
			{
				// further reading not limited by found separator
				var prevOffset = _source.Offset;
				_source.FillBuffer ();
				var resetSearch = (prevOffset != _source.Offset); // изменилась позиция данных в буфере. при этом сами данные, доступные ранее, не могут измениться, могут лишь добавиться новые
				SearchBuffer (resetSearch);
			}
			return (_foundTemplateOffset - _source.Offset);
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
				while ((size > (_foundTemplateOffset - _source.Offset)) && !_source.IsExhausted)
				{
					var prevOffset = _source.Offset;
					_source.FillBuffer ();
					var resetSearch = (prevOffset != _source.Offset); // изменилась позиция данных в буфере. при этом сами данные, доступные ранее, не могут измениться, могут лишь добавиться новые
					SearchBuffer (resetSearch);
				}
				if (size > (_foundTemplateOffset - _source.Offset))
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
		/// True если разделитель найден и пропущен, False если источник исчерпался и разделитель не найден.
		/// </returns>
		public bool TrySkipPart ()
		{
			int sizeToSkip;
			// find separator if not already found
			while (_foundTemplateLength < _template.Length)
			{
				sizeToSkip = _foundTemplateOffset - _source.Offset;
				if (sizeToSkip > 0)
				{
					_source.SkipBuffer (sizeToSkip);
				}
				var prevOffset = _source.Offset;
				_source.FillBuffer ();
				var resetSearch = (prevOffset != _source.Offset); // изменилась позиция данных в буфере. при этом сами данные, доступные ранее, не могут измениться, могут лишь добавиться новые
				if (SearchBuffer (resetSearch))
				{
					// разделитель не найден, источник ичерпался
					var available = _source.Count;
					if (available > 0)
					{
						_source.SkipBuffer (available);
					}
					return false;
				}
			}

			// jump over found separator
			sizeToSkip = _foundTemplateOffset - _source.Offset + _foundTemplateLength;
			_source.SkipBuffer (sizeToSkip);
			SearchBuffer (true);

			return true;
		}

		/// <summary>
		/// Ищет шаблон в текущем буфере.
		/// </summary>
		/// <param name="resetFromStart">Признак сброса поиска.
		/// Если True, то поиск начнется с начала данных буфера,
		/// иначе продолжится с последней позиции где искали в предыдущий раз.</param>
		/// <returns>Если True, то шаблон уже никогда не будет найден (источник исчерпался) и запускать поиск повторно не нужно.</returns>
		protected bool SearchBuffer (bool resetFromStart)
		{
			if (resetFromStart)
			{
				_foundTemplateOffset = _source.Offset;
				_foundTemplateLength = 0;
			}
			var buf = _source.Buffer;
			while (((_foundTemplateOffset + _foundTemplateLength) < (_source.Offset + _source.Count)) && (_foundTemplateLength < _template.Length))
			{
				if (buf[_foundTemplateOffset + _foundTemplateLength] == _template[_foundTemplateLength])
				{
					_foundTemplateLength++;
				}
				else
				{
					_foundTemplateOffset++;
					_foundTemplateLength = 0;
				}
			}
			// no more data from source, separator not found
			if (_source.IsExhausted && (_foundTemplateLength < _template.Length))
			{ // stops searching
				_foundTemplateOffset = _source.Offset + _source.Count;
				_foundTemplateLength = 0;
				return true;
			}
			return false;
		}
	}
}
