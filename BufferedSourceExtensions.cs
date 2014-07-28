using System;
using System.IO;
using System.Text;
using System.Diagnostics.Contracts;

namespace BusinessClassLibrary
{
	/// <summary>
	/// Методы расширения для IBufferedSource.
	/// </summary>
	public static class BufferedSourceExtensions
	{
		#region method IsEmpty

		/// <summary>
		/// Проверяет что указанный источник исчерпан и не содержит данных.
		/// </summary>
		/// <param name="source">Источник данных.</param>
		/// <returns>True если источник исчерпан и не содержит данных.</returns>
		public static bool IsEmpty (this IBufferedSource source)
		{
			if (source == null)
			{
				throw new ArgumentNullException ("source");
			}
			Contract.EndContractBlock ();

			return (source.Count < 1) && source.IsExhausted;
		}

		#endregion

		#region method IndexOf

		/// <summary>
		/// Ищет первое нахождение указанного байта в буфере указанного источника данных,
		/// запрашивая заполнение буфера по необходимости.
		/// </summary>
		/// <param name="source">Источник данных.</param>
		/// <param name="value">Байт-образец, который будет найден в источнике данных.</param>
		/// <returns>Позиция первого нахождения указанного байта в буфере указанного источника данных,
		/// либо -1 если указанный байт в буфере не найден.</returns>
		public static int IndexOf (this IBufferedSource source, byte value)
		{
			if (source == null)
			{
				throw new ArgumentNullException ("source");
			}
			Contract.EndContractBlock ();

			var buffer = source.Buffer;
			var idx = 0;
			var size = source.Count;
			byte bufValue;
			do
			{
				if (idx >= size)
				{
					// весь буфер проверили, дальше надо запросить ещё
					if (source.IsExhausted)
					{
						// источник исчерпался, запрашивать данные больше нет смысла
						return -1;
					}
					var newSize = source.FillBuffer ();
					if (newSize <= size)
					{
						// запрос не добавил новых данных, что означает что в буфер больше ничего не влезет
						return -1;
					}
					size = newSize;
				}
				bufValue = buffer[source.Offset + idx];
				idx++;
			} while (bufValue != value);
			return idx + source.Offset - 1;
		}

		#endregion

		#region method Read

		/// <summary>
		/// Считывает указанное количество байтов в указанный массив по указанному смещению из указанного источника данных.
		/// </summary>
		/// <param name="source">Источник данных.</param>
		/// <param name="buffer">Массив байтов - приёмник данных.
		/// После возврата из метода, буфер в указанном диапазоне будет содержать данные
		/// считанные из источника.</param>
		/// <param name="offset">Позиция в buffer, начиная с которой будут записаны данные, считанные с источника.</param>
		/// <param name="count">Количество байтов, которое нужно считать из источника.</param>
		/// <returns>Актульное количество байтов, помещённых в буфер.
		/// Может быть меньше, чем запрошено, если источник исчерпан.</returns>
		public static int Read (this IBufferedSource source, byte[] buffer, int offset, int count)
		{
			if (source == null)
			{
				throw new ArgumentNullException ("source");
			}
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

			if (count < 1)
			{
				return 0;
			}

			int totalSize = 0;
			do
			{
				var available = source.Count;
				if ((count > available) && !source.IsExhausted)
				{
					available = source.FillBuffer ();
				}
				if (available < 1)
				{
					if (source.IsExhausted)
					{
						break; // end of stream
					}
				}
				else
				{
					var size = Math.Min (available, count);
					Array.Copy (source.Buffer, source.Offset, buffer, offset, size);
					source.SkipBuffer (size);
					offset += size;
					count -= size;
					totalSize += size;
				}
			} while (count > 0);

			return totalSize;
		}

		#endregion

		#region method ReadAllBytes

		/// <summary>
		/// Считывает все данные, оставшиеся в источнике и возвращает их в виде сегмента массива байтов.
		/// </summary>
		/// <param name="source">Источник данных.</param>
		/// <returns>Массив байтов, считанный из источника.</returns>
		/// <remarks>Возвращаемый массив является копией и не связан массивом-буфером источника.</remarks>
		public static byte[] ReadAllBytes (this IBufferedSource source)
		{
			if (source == null)
			{
				throw new ArgumentNullException ("source");
			}
			Contract.EndContractBlock ();

			byte[] result;
			var size = source.FillBuffer ();
			if (source.IsExhausted)
			{
				result = new byte[size];
				if (size > 0)
				{
					Array.Copy (source.Buffer, source.Offset, result, 0, size);
					source.SkipBuffer (size);
				}
			}
			else
			{
				using (var memStream = new MemoryStream (size))
				{
					do
					{
						memStream.Write (source.Buffer, source.Offset, size);
						source.SkipBuffer (size);
						size = source.FillBuffer ();
					} while (size > 0);
					result = memStream.ToArray ();
				}
			}
			return result;
		}

		#endregion

		#region method ReadAllText

		/// <summary>
		/// Считывает все данные, оставшиеся в источнике и возвращает их в виде строки.
		/// </summary>
		/// <param name="source">Источник данных.</param>
		/// <param name="encoding">Кодировка, используемая для конвертации байтов в строку.</param>
		/// <returns>Строка, считанная из источника.</returns>
		public static string ReadAllText (this IBufferedSource source, Encoding encoding)
		{
			if (source == null)
			{
				throw new ArgumentNullException ("source");
			}
			if (encoding == null)
			{
				throw new ArgumentNullException ("encoding");
			}
			Contract.EndContractBlock ();

			// обязательно читать полностью всё потому что неизвестно сколько байт занимают отдельные символы
			var buf = ReadAllBytes (source);
			return (buf.Length < 1) ? string.Empty : encoding.GetString (buf, 0, buf.Length);
		}

		#endregion

		#region method WriteTo

		/// <summary>
		/// Сохраняет содержимое указанного источника данных в указанный поток.
		/// </summary>
		/// <param name="source">Источник данных, содержимое которого будет сохранено в указанный поток.</param>
		/// <param name="destination">Поток, в который будет сохранено содержимое указанного источника данных.</param>
		/// <returns>Количество байтов, записанный в указанный поток.</returns>
		public static long WriteTo (this IBufferedSource source, Stream destination)
		{
			if (source == null)
			{
				throw new ArgumentNullException ("source");
			}
			if (destination == null)
			{
				throw new ArgumentNullException ("destination");
			}
			if (!destination.CanWrite)
			{
				throw new ArgumentOutOfRangeException ("destination");
			}
			Contract.EndContractBlock ();

			long resultSize = 0;
			int available;
			while ((available = source.FillBuffer ()) > 0)
			{
				destination.Write (source.Buffer, source.Offset, available);
				resultSize += available;
				source.SkipBuffer (available);
			}

			return resultSize;
		}

		#endregion
	}
}
