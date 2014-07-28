using System;
using System.Diagnostics.Contracts;
using ICryptographicTransform = System.Security.Cryptography.ICryptoTransform;

namespace BusinessClassLibrary
{
	/// <summary>
	/// Источник данных, представленный байтовым буфером,
	/// применяющий криптографическое преобразование к данным другого источника.
	/// </summary>
	[System.Diagnostics.DebuggerDisplay ("{Offset}...{Offset+Count} ({Buffer.Length}) exhausted={IsExhausted}")]
	public class CryptoTransformingBufferedSource :
		IBufferedSource
	{
		private readonly IBufferedSource _source;
		private readonly ICryptographicTransform _cryptoTransform;
		private readonly int _inputMaxBlocks;
		private readonly byte[] _buffer;
		private int _offset = 0;
		private int _count = 0;
		private bool _sourceEnded = false;
		private bool _isExhausted = false;
		private byte[] _cache = null;
		private int _cacheStartOffset;
		private int _cacheEndOffset;

		/// <summary>
		/// Инициализирует новый экземпляр CryptoTransformingBufferedSource получающий данные из указанного источника
		/// и применяющий к ним указанное криптографическое преобразование.
		/// </summary>
		/// <param name="source">Источник данных, к которому будет применяться криптографическое преобразование.</param>
		/// <param name="cryptoTransform">Криптографическое преобразование, которое будет применяться к данным источника.</param>
		/// <param name="buffer">
		/// Байтовый буфер, в котором будут содержаться преобразованные данные.
		/// Должен быть достаточен по размеру,
		/// чтобы вмещать выходной блок криптографического преобразования (cryptoTransform.OutputBlockSize).
		/// </param>
		public CryptoTransformingBufferedSource (IBufferedSource source, ICryptographicTransform cryptoTransform, byte[] buffer)
		{
			if (source == null)
			{
				throw new ArgumentNullException ("source");
			}
			if (cryptoTransform == null)
			{
				throw new ArgumentNullException ("cryptoTransform");
			}
			if (buffer == null)
			{
				throw new ArgumentNullException ("cryptoTransform");
			}
			if (buffer.Length < 1)
			{
				throw new ArgumentOutOfRangeException ("buffer");
			}
			if (buffer.Length < cryptoTransform.OutputBlockSize)
			{
				throw new ArgumentOutOfRangeException ("buffer", string.Format (System.Globalization.CultureInfo.InvariantCulture,
					"buffer.Length ({0}) less than cryptoTransform.OutputBlockSize ({1}).",
					buffer.Length,
					cryptoTransform.OutputBlockSize));
			}
			Contract.EndContractBlock ();

			_source = source;
			_cryptoTransform = cryptoTransform;
			_inputMaxBlocks = _source.Buffer.Length / _cryptoTransform.InputBlockSize;
			_buffer = buffer;
		}

		/// <summary>
		/// Получает буфер, в котором содержится некоторая часть данных источника.
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

		/// <summary>Получает признак исчерпания источника.
		/// Возвращает True если источник больше не поставляет данных.
		/// Содержимое буфера при этом остаётся верным, но больше не будет меняться.</summary>
		public bool IsExhausted { get { return _isExhausted; } }

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

			// TODO: тут вроде возможна какая то оптимизация вместо бесхитростного последовательного трансформирования
			int readed;
			while ((readed = FillBuffer ()) > 0)
			{
				if (size <= (long)readed)
				{
					// делаем доступным остаток буфера
					_offset += (int)size;
					_count -= (int)size;
					skipped += size;
					return skipped;
				}
				size -= (long)readed;
				skipped += readed;
				_offset = _count = 0;
			}
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
			if (!_isExhausted && (_count < _buffer.Length))
			{
				Defragment ();

				int sizeTransformed;
				do
				{
					sizeTransformed = LoadFromCache ();
					if (sizeTransformed < 1)
					{
						var sourceAvailableSize = _source.Count;
						var sourceSizeNeeded = GetInputSizeToFillOutputSize (_buffer.Length - _offset - _count);
						if (((_cryptoTransform.CanTransformMultipleBlocks ? sourceSizeNeeded : _cryptoTransform.InputBlockSize) > sourceAvailableSize) && !_source.IsExhausted)
						{
							if ((_source.FillBuffer () < _cryptoTransform.InputBlockSize) && !_source.IsExhausted)
							{
								throw new InvalidOperationException (string.Format (System.Globalization.CultureInfo.InvariantCulture,
									"Source (buffer size={0}) can't provide enough data to transform single block (size={1}).",
									_source.Buffer.Length,
									_cryptoTransform.InputBlockSize));
							}
						}
						sizeTransformed = LoadFromTransformedSource ();
					}
					Accept (sizeTransformed);
				} while (!_isExhausted && (sizeTransformed < 1)); // повторяем пока трансформация не вернет хотя бы один байт результата
			}
			return _count;
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
		/// </exception>
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

				if ((shortage > 0) && !_isExhausted)
				{
					Defragment ();

					while ((shortage > 0) && !_isExhausted)
					{
						int sizeTransformed = LoadFromCache ();
						if (sizeTransformed < 1)
						{
							var sourceAvailableSize = _source.Count;
							var sourceSizeNeeded = GetInputSizeToFillOutputSize (_buffer.Length - _offset - _count);
							if (((_cryptoTransform.CanTransformMultipleBlocks ? sourceSizeNeeded : _cryptoTransform.InputBlockSize) > sourceAvailableSize) && !_source.IsExhausted)
							{
								if ((_source.FillBuffer () < _cryptoTransform.InputBlockSize) && !_source.IsExhausted)
								{
									throw new InvalidOperationException (string.Format (System.Globalization.CultureInfo.InvariantCulture,
										"Source (buffer size={0}) can't provide enough data to transform single block (size={1}).",
										_source.Buffer.Length,
										_cryptoTransform.InputBlockSize));
								}
							}

							sizeTransformed = LoadFromTransformedSource ();
						}
						Accept (sizeTransformed);
						shortage -= sizeTransformed;
					}
				}
				if (shortage > 0)
				{
					throw new InvalidOperationException ("Source exhausted and can not provide requested size of data.");
				}
			}
		}

		/// <summary>
		/// Получает размер данных источника, необходимых чтобы заполнить указанное количество байтов результата трансформации.
		/// <param name="availableSize">Количество байтов, доступных для результата трансформации.</param>
		/// </summary>
		protected int GetInputSizeToFillOutputSize (int availableSize)
		{
			if (_inputMaxBlocks < 1)
			{ // входной буфер меньше одного блока транформации, запрашиваем весь буфер
				return _source.Buffer.Length;
			}

			var outputAvailableBlocks = availableSize / _cryptoTransform.OutputBlockSize;

			var neededBlocks = Math.Min (outputAvailableBlocks, _inputMaxBlocks);
			if (neededBlocks < 1)
			{
				// минимум один блок
				neededBlocks = 1;
			}
			return neededBlocks * _cryptoTransform.InputBlockSize;
		}

		/// <summary>
		/// Принимает в буфер указанное количество трансформированных данных.
		/// Добавленные данные должны располагаться в буфере начиная с позиции Count.
		/// </summary>
		/// <param name="size">Количество трансформированных байтов данных.</param>
		protected void Accept (int size)
		{
			_count += size;
		}

		/// <summary>
		/// Обеспечивает чтобы данные в буфере начинались с позиции ноль.
		/// </summary>
		protected void Defragment ()
		{
			if (_offset > 0)
			{
				if (_count > 0)
				{
					Array.Copy (_buffer, _offset, _buffer, 0, _count);
				}
				_offset = 0;
			}
		}

		/// <summary>
		/// Запрашивает данные из кэша.
		/// </summary>
		/// <returns>Количество байтов кэша, помещенных в выходной буфер.</returns>
		protected int LoadFromCache ()
		{
			int size = 0;
			if (_cache != null)
			{
				var outputAvailableSize = _buffer.Length - _offset - _count;
				var cacheAvailableSize = _cacheEndOffset - _cacheStartOffset;
				size = Math.Min (outputAvailableSize, cacheAvailableSize);
				Array.Copy (_cache, _cacheStartOffset, _buffer, _offset + _count, size);
				_cacheStartOffset += size;
				if (_cacheStartOffset >= _cacheEndOffset)
				{
					_cache = null;
					if (_sourceEnded)
					{
						_isExhausted = true;
					}
				}
			}
			return size;
		}

		/// <summary>
		/// Запрашивает трансформированные данные источника.
		/// </summary>
		/// <returns>Количество байтов, помещенных в выходной буфер.</returns>
		protected int LoadFromTransformedSource ()
		{
			var sourceAvailableSize = _source.Count;
			var outputAvailableSize = _buffer.Length - _offset - _count;
			int sizeTransformed = 0;
			if (sourceAvailableSize >= _cryptoTransform.InputBlockSize)
			{// в источнике есть как минимум один входной блок, продолжаем преобразование
				var outputAvailableBlocks = outputAvailableSize / _cryptoTransform.OutputBlockSize;
				if (outputAvailableBlocks > 0)
				{ // остаток буфера достаточен для выходного блока
					int sourceBlocksNeeded = 1;
					if (_cryptoTransform.CanTransformMultipleBlocks)
					{
						var sourceBlocksAvailable = sourceAvailableSize / _cryptoTransform.InputBlockSize;
						sourceBlocksNeeded = Math.Min (sourceBlocksAvailable, outputAvailableBlocks);
					}
					var sourceSizeNeeded = sourceBlocksNeeded * _cryptoTransform.InputBlockSize;
					sizeTransformed = _cryptoTransform.TransformBlock (
						_source.Buffer,
						_source.Offset,
						sourceSizeNeeded,
						_buffer,
						_offset + _count);
					_source.SkipBuffer (sourceSizeNeeded);
				}
				else
				{ // остаток буфера мал для выходного блока, трансформируем один блок в кэш
					var sourceSizeNeeded = _cryptoTransform.InputBlockSize;
					var cache = new byte[_cryptoTransform.OutputBlockSize];
					sizeTransformed = _cryptoTransform.TransformBlock (
						_source.Buffer,
						_source.Offset,
						sourceSizeNeeded,
						cache,
						0);
					_source.SkipBuffer (sourceSizeNeeded);
					if (sizeTransformed > outputAvailableSize)
					{ // поскольку весь буфер не влезает, сохраняем его остаток в кэше
						_cache = cache;
						_cacheStartOffset = outputAvailableSize;
						_cacheEndOffset = sizeTransformed;
						sizeTransformed = outputAvailableSize;
					}
					Array.Copy (cache, 0, _buffer, _offset + _count, sizeTransformed);
				}
			}
			else
			{// в источнике меньше чем один входной блок, завершаем преобразование
				_sourceEnded = true;
				var finalBlock = _cryptoTransform.TransformFinalBlock (_source.Buffer, _source.Offset, sourceAvailableSize);

				if (sourceAvailableSize > 0)
				{
					_source.SkipBuffer (sourceAvailableSize);
				}
				if (finalBlock.Length > outputAvailableSize)
				{ // поскольку весь буфер не влезает, сохраняем его остаток в кэше
					_cache = finalBlock;
					_cacheStartOffset = outputAvailableSize;
					_cacheEndOffset = finalBlock.Length;
					sizeTransformed = outputAvailableSize;
				}
				else
				{
					sizeTransformed = finalBlock.Length;
					_isExhausted = true;
				}
				Array.Copy (finalBlock, 0, _buffer, _offset + _count, sizeTransformed);
			}
			return sizeTransformed;
		}
	}
}
