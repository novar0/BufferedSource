using System;
using System.Diagnostics.Contracts;

namespace BusinessClassLibrary
{
	/// <summary>
	/// Источник данных, представленный байтовым буфером
	/// в качестве которого используется предоставленный массив байтов.
	/// </summary>
	[System.Diagnostics.DebuggerDisplay ("{Offset}...{Offset+Count} ({Buffer.Length}) exhausted={IsExhausted}")]
	public class ArrayBufferedSource :
		IBufferedSource
	{
		private readonly byte[] _buffer;
		private int _offset;
		private int _count;

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

		/// <summary>Возвращает True, потому что исходный массив неизменен.</summary>
		public bool IsExhausted { get { return true; } }

		/// <summary>
		/// Инициализирует новый экземпляр ArrayBufferedSource использующий в качестве буфера предоставленный массив байтов.
		/// </summary>
		/// <param name="buffer">Массив байтов, который будет буфером источника.</param>
		public ArrayBufferedSource (byte[] buffer)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException ("buffer");
			}
			Contract.EndContractBlock ();

			_buffer = buffer;
			_offset = 0;
			_count = buffer.Length;
		}

		/// <summary>
		/// Инициализирует новый экземпляр ArrayBufferedSource использующий в качестве буфера предоставленный сегмента массива байтов.
		/// </summary>
		/// <param name="buffer">Массив байтов, который будет буфером источника.</param>
		/// <param name="offset">Позиция начала данных в buffer.</param>
		/// <param name="count">Количество байтов в buffer.</param>
		public ArrayBufferedSource (byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException ("buffer");
			}
			if ((offset < 0) || (offset > buffer.Length) || ((offset == buffer.Length) && (count > 0)))
			{
				throw new ArgumentOutOfRangeException ("offset");
			}
			if ((count < 0) || ((offset + count) > buffer.Length))
			{
				throw new ArgumentOutOfRangeException ("count");
			}
			Contract.EndContractBlock ();

			_buffer = buffer;
			_offset = offset;
			_count = count;
		}

		/// <summary>
		/// Отбрасывает (пропускает) указанное количество данных из начала буфера.
		/// При выполнении может измениться свойство Offset.
		/// </summary>
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

		/// <summary>Возвращает размер доступных в буфере данных.</summary>
		/// <returns>Размер доступных в буфере данных.</returns>
		public int FillBuffer ()
		{
			return _count;
		}

		/// <summary>Проверяет, что источник может предоставить указанное количество данных.</summary>
		/// <param name="size">Требуемый размер данных в буфере.</param>
		public void EnsureBuffer (int size)
		{
			if ((size < 0) || (size > this.Buffer.Length))
			{
				throw new ArgumentOutOfRangeException ("size");
			}
			Contract.EndContractBlock ();

			if (size > _count)
			{
				throw new InvalidOperationException ("Source exhausted and can not provide requested size of data.");
			}
		}

		/// <summary>
		/// Пытается пропустить указанное количество доступных в буфере данных.
		/// При выполнении могут измениться свойства Offset и Count.
		/// </summary>
		/// <param name="size">Количество байтов данных для пропуска.</param>
		/// <returns>
		/// Количество пропущеных в буфере байтов данных. Может быть меньше, чем было указано, если данные закончились.
		/// </returns>
		public long TrySkip (long size)
		{
			if (size < 0L)
			{
				throw new ArgumentOutOfRangeException ("size");
			}
			Contract.EndContractBlock ();

			var available = _count;
			if (size > (long)available)
			{
				_offset = _count = 0;
				return (long)available;
			}
			_offset += (int)size;
			_count -= (int)size;
			return size;
		}
	}
}
