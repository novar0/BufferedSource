using System;
using System.IO;
using System.Diagnostics.Contracts;

namespace BusinessClassLibrary.IO
{
	/// <summary>
	/// Поток для последовательного чтения,
	/// получающий данные из указанного источника данных,
	/// представленного байтовым буфером.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage ("Microsoft.Naming",
		"CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
		Justification = "'Stream' suffix intended because of base type is System.IO.Stream.")]
	public class BufferedSourceStream : Stream
	{
		private readonly IBufferedSource _source;

		/// <summary>
		/// Инициализирует новый экземпляр BufferedSourceStream,
		/// получающий данные из указанного источника данных.
		/// </summary>
		/// <param name="source">Источник данных, содержимое которого станет содержимым потока.</param>
		public BufferedSourceStream (IBufferedSource source)
		{
			if (source == null)
			{
				throw new ArgumentNullException ("source");
			}
			Contract.EndContractBlock ();

			_source = source;
		}

		/// <summary>
		/// Возвращает True, означающее что поток поддерживает возможность чтения.
		/// </summary>
		public override bool CanRead { get { return true; } }

		/// <summary>
		/// Возвращает False, означающее что поток не поддерживает возможность записи.
		/// </summary>
		public override bool CanWrite { get { return false; } }

		/// <summary>
		/// Возвращает False, означающее что поток не поддерживает возможность поиска.
		/// </summary>
		public override bool CanSeek { get { return false; } }

		/// <summary>
		/// Получение длины потока не поддерживается.
		/// </summary>
		/// <exception cref="System.NotSupportedException" />
		public override long Length { get { throw new NotSupportedException (); } }

		/// <summary>
		/// Получение и задание позиции не поддерживается.
		/// </summary>
		/// <exception cref="System.NotSupportedException" />
		public override long Position { get { throw new NotSupportedException (); } set { throw new NotSupportedException (); } }

		/// <summary>
		/// Задание позиции не поддерживается.
		/// </summary>
		/// <param name="offset">Не используется.</param>
		/// <param name="origin">Не используется.</param>
		/// <exception cref="System.NotSupportedException" />
		public override long Seek (long offset, SeekOrigin origin) { throw new NotSupportedException (); }

		/// <summary>
		/// Задание длины не поддерживается.
		/// </summary>
		/// <param name="value">Не используется.</param>
		/// <exception cref="System.NotSupportedException" />
		public override void SetLength (long value) { throw new NotSupportedException (); }

		/// <summary>
		/// Запись не поддерживается.
		/// </summary>
		/// <param name="buffer">Не используется.</param>
		/// <param name="offset">Не используется.</param>
		/// <param name="count">Не используется.</param>
		/// <exception cref="System.NotSupportedException" />
		public override void Write (byte[] buffer, int offset, int count) { throw new NotSupportedException (); }

		/// <summary>
		/// Ничего не делает потому что поток не поддерживает возможность записи.
		/// </summary>
		public override void Flush () { }

		/// <summary>
		/// Считывает последовательность байтов из текущего потока и перемещает позицию в потоке на число считанных байтов.
		/// </summary>
		/// <param name="buffer">
		/// Массив байтов. После завершения выполнения данного метода буфер содержит указанный массив байтов,
		/// в котором значения в интервале между offset и (offset + count - 1) заменены байтами, считанными из текущего источника.
		/// </param>
		/// <param name="offset">
		/// Смещение байтов (начиная с нуля) в buffer, с которого начинается сохранение данных, считанных из текущего потока.
		/// </param>
		/// <param name="count">Максимальное количество байтов, которое должно быть считано из текущего потока.</param>
		/// <returns>Количество байтов, считанных в буфер.
		/// Может быть меньше количества запрошенных байтов, если столько байтов в настоящее время недоступно,
		/// а также равняться нулю (0), если был достигнут конец потока.</returns>
		public override int Read (byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException ("buffer");
			}
			if ((offset < 0) || (offset > buffer.Length) || ((offset == buffer.Length) && (count > 0)))
			{
				throw new ArgumentOutOfRangeException ("offset");
			}
			if ((count < 0) || (count > (buffer.Length - offset)))
			{
				throw new ArgumentOutOfRangeException ("count");
			}
			Contract.EndContractBlock ();

			int resultSize = 0;
			int available;
			while ((count > 0) && ((available = _source.FillBuffer ()) > 0))
			{
				var toCopy = Math.Min (available, count);
				Array.Copy (_source.Buffer, _source.Offset, buffer, offset, toCopy);
				offset += toCopy;
				count -= toCopy;
				resultSize += toCopy;
				_source.SkipBuffer (toCopy);
			}

			return resultSize;
		}

		/// <summary>
		/// Считывает байт из потока и перемещает позицию в потоке на один байт или возвращает -1, если достигнут конец потока.
		/// </summary>
		/// <returns>Байт без знака, приведенный к Int32, или значение -1, если достигнут конец потока.</returns>
		public override int ReadByte ()
		{
			if (_source.Count < 1)
			{
				var available = _source.FillBuffer ();
				if (available < 1)
				{
					return -1;
				}
			}
			var result = (int)_source.Buffer[_source.Offset];
			_source.SkipBuffer (1);
			return result;
		}
	}
}
