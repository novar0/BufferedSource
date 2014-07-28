using System;
using System.Diagnostics.Contracts;

namespace BusinessClassLibrary
{
	/// <summary>
	/// Ретранслятор, дублирующий источник, представленный байтовым буфером,
	/// отправляющий уведомления о потреблённых из источника данных.
	/// </summary>
	[System.Diagnostics.DebuggerDisplay ("{Offset}...{Offset+Count} ({Buffer.Length}) exhausted={IsExhausted}")]
	public class ObservableBufferedSource :
		IBufferedSource
	{
		private readonly IBufferedSource _source;
		private readonly IProgress<long> _progress;

		/// <summary>
		/// Инициализирует новый экземпляр ObservableBufferedSource,
		/// который будет ретранслировать указанный источник.
		/// </summary>
		/// <param name="source">Источник данных, который будет ретранслироваться.</param>
		/// <param name="progress">Объект, который будет получать уведомления о потреблении данных источника.</param>
		[CLSCompliant (false)]
		public ObservableBufferedSource (IBufferedSource source, IProgress<long> progress)
		{
			if (source == null)
			{
				throw new ArgumentNullException ("source");
			}
			if (progress == null)
			{
				throw new ArgumentNullException ("progress");
			}
			Contract.EndContractBlock ();

			_source = source;
			_progress = progress;
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
		public int Count { get { return _source.Count; } }

		/// <summary>
		/// Получает признак исчерпания источника.
		/// Возвращает True если источник больше не поставляет данных.
		/// Содержимое буфера при этом остаётся верным, но больше не будет меняться.
		/// </summary>
		public bool IsExhausted { get { return _source.IsExhausted; } }

		/// <summary>
		/// Заполняет буфер данными источника, дополняя уже доступные там данные.
		/// В результате буфер может быть заполнен неполностью если источник поставляет данные блоками, либо пуст если источник исчерпался.
		/// При выполнении могут измениться свойства Offset, Count и IsExhausted.
		/// </summary>
		/// <returns>Размер доступных в буфере данных. Если ноль, то источник исчерпан и доступных данных в буфере больше не будет.</returns>
		public int FillBuffer () { return _source.FillBuffer (); }

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
				_source.EnsureBuffer (size);
			}
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
				_source.SkipBuffer (size);
				_progress.Report (size);
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
			var result = _source.TrySkip (size);
			_progress.Report (result);
			return result;
		}
	}
}
