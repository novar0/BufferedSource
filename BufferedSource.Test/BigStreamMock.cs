using System;
using System.IO;

namespace BusinessClassLibrary.Test
{
	// имитирует поток большого размера без выделения ресурсов
	// чтение заполняет буфер путем вызова указанной функции, в которую передается абсолютная позиция в потоке
	internal class BigStreamMock : Stream
	{
		private readonly bool _canSeek;
		private readonly long _length;
		private readonly Func<long, byte> _dataFunction;
		private long _position = 0;

		internal BigStreamMock (long length, bool canSeek, Func<long, byte> dataFunction)
			: base ()
		{
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException ("inputOffset");
			}
			if (dataFunction == null)
			{
				throw new ArgumentNullException ("dataFunction");
			}

			_length = length;
			_canSeek = canSeek;
			_dataFunction = dataFunction;
		}

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return _canSeek; }
		}

		public override bool CanWrite
		{
			get { return false; }
		}


		public override long Length
		{
			get
			{
				if (!_canSeek) throw new NotSupportedException ();
				return _length;
			}
		}

		public override long Position
		{
			get
			{
				return _position;
			}
			set
			{
				if (!_canSeek) throw new NotSupportedException ();
				_position = value;
			}
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			long available = _length - _position;
			int readed = 0;
			if (available > 0)
			{
				readed = (int)Math.Min (available, (long)count);
				for (int i = 0; i < readed; i++)
				{
					buffer[offset + i] = _dataFunction (_position + (long)i);
				}
				_position += readed;
			}
			return readed;
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			if (!_canSeek) throw new NotSupportedException ();
			switch (origin)
			{
				case SeekOrigin.Begin:
					_position = offset;
					break;
				case SeekOrigin.Current:
					_position += offset;
					break;
				case SeekOrigin.End:
					_position += _length + offset;
					break;
			}
			if (_position < 0) _position = 0;
			return _position;
		}

		public override void Flush () { throw new NotSupportedException (); }
		public override void SetLength (long value) { throw new NotSupportedException (); }
		public override void Write (byte[] buffer, int offset, int count) { throw new NotSupportedException (); }
	}
}
