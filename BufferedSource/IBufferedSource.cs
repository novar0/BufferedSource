using System.Diagnostics.Contracts;

namespace BusinessClassLibrary
{
	/// <summary>
	/// Источник данных для последовательного чтения, представленный байтовым буфером.
	/// </summary>
	[ContractClass (typeof (IBufferedSourceContracts))]
	public interface IBufferedSource
	{
		/// <summary>
		/// Получает буфер, в котором содержится некоторая часть данных источника.
		/// Текущая начальная позиция и количество доступных данных содержатся в свойствах Offset и Count,
		/// при этом сам буфер остаётся неизменным всё время жизни источника.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage ("Microsoft.Performance",
			"CA1819:PropertiesShouldNotReturnArrays",
			Justification = "This is clearly a property and write access to array is intended.")]
		byte[] Buffer { get; }

		/// <summary>
		/// Получает начальную позицию данных, доступных в Buffer.
		/// Количество данных, доступных в Buffer, содержится в Count.
		/// </summary>
		int Offset { get; }

		/// <summary>
		/// Получает количество данных, доступных в Buffer.
		/// Начальная позиция доступных данных содержится в Offset.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Получает признак исчерпания источника.
		/// Возвращает True если источник больше не поставляет данных.
		/// Содержимое буфера при этом остаётся верным, но больше не будет меняться.
		/// </summary>
		bool IsExhausted { get; }

		/// <summary>
		/// Заполняет буфер данными источника, дополняя уже доступные там данные.
		/// В результате буфер может быть заполнен неполностью если источник поставляет данные блоками, либо пуст если источник исчерпался.
		/// При выполнении могут измениться свойства Offset, Count и IsExhausted.
		/// </summary>
		/// <returns>Размер доступных в буфере данных. Если ноль, то источник исчерпан и доступных данных в буфере больше не будет.</returns>
		int FillBuffer ();

		/// <summary>
		/// Запрашивает у источника указанное количество данных в буфере.
		/// В результате запроса в буфере может оказаться данных больше, чем запрошено.
		/// При выполнении могут измениться свойства Offset, Count и IsExhausted.
		/// </summary>
		/// <param name="size">Требуемый размер данных в буфере.</param>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// Происходит если size меньше нуля или больше размера буфера.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Происходит если источник не смог предоставить в буфере указанное количество данных.
		/// </exception>
		void EnsureBuffer (int size);

		/// <summary>
		/// Пропускает указанное количество данных из начала доступных данных буфера.
		/// При выполнении может измениться свойство Offset.
		/// </summary>
		/// <param name="size">Размер данных для пропуска в начале доступных данных буфера.
		/// Должен быть меньше чем размер доступных в буфере данных.</param>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// Происходит если size меньше нуля или больше размера доступных в буфере данных.
		/// </exception>
		void SkipBuffer (int size);

		/// <summary>
		/// Пытается пропустить указанное количество данных источника, включая доступные в буфере данные.
		/// При выполнении могут измениться свойства Offset, Count и IsExhausted.
		/// </summary>
		/// <param name="size">Количество байтов данных для пропуска, включая доступные в буфере данные.</param>
		/// <returns>
		/// Количество пропущеных байтов данных, включая доступные в буфере данные.
		/// Может быть меньше, чем было указано, если источник исчерпался.
		/// После выполнения попытки, независимо от её результата, источник будет предоставлять данные, идущие сразу за пропущенными.
		/// </returns>
		/// <exception cref="System.ArgumentOutOfRangeException">Происходит если size меньше нуля.</exception>
		long TrySkip (long size);
	}

	/// <summary>
	/// Содержит только метаданные контрактов для IBufferedSource.
	/// </summary>
	[ContractClassFor (typeof (IBufferedSource))]
	internal abstract class IBufferedSourceContracts :
		IBufferedSource
	{
		private IBufferedSourceContracts () { }

		public byte[] Buffer { get { return null; } }

		public int Offset { get { return 0; } }

		public int Count { get { return 0; } }

		public bool IsExhausted { get { return false; } }

		public int FillBuffer ()
		{
			Contract.Ensures (this.Buffer == Contract.OldValue (this.Buffer));
			Contract.Ensures ((Contract.Result<int> () > 0) || this.IsExhausted);
			Contract.Ensures (Contract.Result<int> () == this.Count);
			Contract.EndContractBlock ();
			return 0;
		}

		public void EnsureBuffer (int size)
		{
			Contract.Requires (size >= 0);
			Contract.Requires (size <= Buffer.Length);
			Contract.Ensures (this.Buffer == Contract.OldValue (this.Buffer));
			Contract.EndContractBlock ();
		}

		public void SkipBuffer (int size)
		{
			Contract.Requires ((size >= 0) && (size <= this.Count));
			Contract.Ensures (this.Buffer == Contract.OldValue (this.Buffer));
			Contract.Ensures ((this.Offset + this.Count) == (Contract.OldValue (this.Offset) + Contract.OldValue (this.Count)));
			Contract.Ensures (this.IsExhausted == Contract.OldValue (this.IsExhausted));
			Contract.EndContractBlock ();
		}

		public long TrySkip (long size)
		{
			Contract.Requires (size >= 0);
			Contract.Ensures (this.Buffer == Contract.OldValue (this.Buffer));
			Contract.EndContractBlock ();
			return 0L;
		}
	}
}
