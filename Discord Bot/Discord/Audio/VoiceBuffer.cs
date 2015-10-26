﻿using System;
using System.Threading;

namespace Discord.Audio
{
    internal class VoiceBuffer : IDiscordVoiceBuffer
	{
		private readonly int _frameSize, _frameCount, _bufferSize;
		private readonly byte[] _buffer;
		private readonly byte[] _blankFrame;
		private ushort _readCursor, _writeCursor;
		private ManualResetEventSlim _underflowEvent, _notOverflowEvent;
		private bool _isClearing;

		public int FrameSize => _frameSize;
		public int FrameCount => _frameCount;
		public ushort ReadPos => _readCursor;
		public ushort WritePos => _readCursor;

		public VoiceBuffer(int frameCount, int frameSize)
		{
			_frameSize = frameSize;
			_frameCount = frameCount;
			_bufferSize = _frameSize * _frameCount;
            _readCursor = 0;
			_writeCursor = 0;
			_buffer = new byte[_bufferSize];
			_blankFrame = new byte[_frameSize];
			_underflowEvent = new ManualResetEventSlim(); //Notifies when an underflow has occurred
			_notOverflowEvent = new ManualResetEventSlim(); //Notifies when an overflow is solved
        }

		public void Push(byte[] buffer, int bytes, CancellationToken cancelToken)
		{
			if (cancelToken.IsCancellationRequested)
				throw new OperationCanceledException("Client is disconnected.", cancelToken);

            int wholeFrames = bytes / _frameSize;
			int expectedBytes = wholeFrames * _frameSize;
			int lastFrameSize = bytes - expectedBytes;

			lock (this)
			{
				for (int i = 0, pos = 0; i <= wholeFrames; i++, pos += _frameSize)
				{
					//If the read cursor is in the next position, wait for it to move.
					ushort nextPosition = _writeCursor;
					AdvanceCursorPos(ref nextPosition);
					if (_readCursor == nextPosition)
					{
						_notOverflowEvent.Reset();
						try
						{
							_notOverflowEvent.Wait(cancelToken);
						}
						catch (OperationCanceledException ex)
						{
							throw new OperationCanceledException("Client is disconnected.", ex, cancelToken);
						}
					}

					if (i == wholeFrames)
					{
						//If there are no partial frames, skip this step
						if (lastFrameSize == 0)
							break;

						//Copy partial frame
						Buffer.BlockCopy(buffer, pos, _buffer, _writeCursor * _frameSize, lastFrameSize);

						//Wipe the end of the buffer
						Buffer.BlockCopy(_blankFrame, 0, _buffer, _writeCursor * _frameSize + lastFrameSize, _frameSize - lastFrameSize);
					}
					else
					{
						//Copy full frame
						Buffer.BlockCopy(buffer, pos, _buffer, _writeCursor * _frameSize, _frameSize);
					}

					//Advance the write cursor to the next position
					AdvanceCursorPos(ref _writeCursor);
					_underflowEvent.Set();
                }
			}
		}

		public bool Pop(byte[] buffer)
		{
            if (_writeCursor == _readCursor)
			{
				_underflowEvent.Set();
				_notOverflowEvent.Set();
				return false;
			}

			bool isClearing = _isClearing;
			if (!isClearing)
				Buffer.BlockCopy(_buffer, _readCursor * _frameSize, buffer, 0, _frameSize);

			//Advance the read cursor to the next position
			AdvanceCursorPos(ref _readCursor);
			_notOverflowEvent.Set();
			return !isClearing;
		}

		public void Clear(CancellationToken cancelToken)
		{
			lock (this)
			{
				_isClearing = true;
                for (int i = 0; i < _frameCount; i++)
					Buffer.BlockCopy(_blankFrame, 0, _buffer, i * _frameCount, i++);
				try
				{
					_underflowEvent.Wait(cancelToken);
				}
				catch (OperationCanceledException) { }
				_writeCursor = 0;
				_readCursor = 0;
				_isClearing = false;
            }
		}

		public void Wait(CancellationToken cancelToken)
		{
			_underflowEvent.Wait(cancelToken);
		}

		private void AdvanceCursorPos(ref ushort pos)
		{
			pos++;
			if (pos == _frameCount)
				pos = 0;
		}
	}
}