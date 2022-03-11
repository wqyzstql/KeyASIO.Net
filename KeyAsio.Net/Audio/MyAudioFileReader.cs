﻿using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KeyAsio.Net.Audio;

public class MyAudioFileReader : WaveStream, ISampleProvider
{
    private WaveStream _readerStream;
    private readonly NAudio.Wave.SampleProviders.SampleChannel _sampleChannel;
    private readonly int _destBytesPerSample;
    private readonly int _sourceBytesPerSample;
    private readonly long _length;
    private readonly object _lockObject;

    /// <summary>
    /// Initializes a new instance of AudioFileReader
    /// </summary>
    /// <param name="fileName">The file to open</param>
    public MyAudioFileReader(string fileName)
    {
        _lockObject = new object();
        FileName = fileName;
        CreateReaderStream(fileName);
        _sourceBytesPerSample = _readerStream!.WaveFormat.BitsPerSample / 8 * _readerStream.WaveFormat.Channels;
        _sampleChannel = new NAudio.Wave.SampleProviders.SampleChannel(_readerStream, false);
        _destBytesPerSample = 4 * _sampleChannel.WaveFormat.Channels;
        _length = SourceToDest(_readerStream.Length);
    }

    public MyAudioFileReader(Stream stream, WaveType waveType)
    {
        _lockObject = new object();
        CreateReaderStream(stream, waveType);

        _sourceBytesPerSample = _readerStream!.WaveFormat.BitsPerSample / 8 * _readerStream.WaveFormat.Channels;
        _sampleChannel = new NAudio.Wave.SampleProviders.SampleChannel(_readerStream, false);
        _destBytesPerSample = 4 * _sampleChannel.WaveFormat.Channels;
        _length = SourceToDest(_readerStream.Length);
    }

    /// <summary>
    /// Initializes a new instance of AudioFileReader
    /// </summary>
    /// <param name="buffer">The stream to open</param>
    /// <param name="waveType">The type of source stream</param>
    public MyAudioFileReader(byte[] buffer, WaveType waveType)
        : this(new MemoryStream(buffer), waveType)
    {
    }

    private void CreateReaderStream(Stream sourceStream, WaveType waveType)
    {
        switch (waveType)
        {
            case WaveType.Wav:
                {
                    _readerStream = new WaveFileReader(sourceStream);
                    if (_readerStream.WaveFormat.Encoding == WaveFormatEncoding.Pcm ||
                        _readerStream.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                        return;
                    _readerStream = WaveFormatConversionStream.CreatePcmStream(_readerStream);
                    _readerStream = new BlockAlignReductionStream(_readerStream);
                    break;
                }
            case WaveType.Mp3:
                _readerStream = new Mp3FileReader(sourceStream);
                break;
            case WaveType.Aiff:
                _readerStream = new AiffFileReader(sourceStream);
                break;
            default:
                throw new NotSupportedException("Not support the specific stream type");
        }
    }

    /// <summary>
    /// Creates the reader stream, supporting all filetypes in the core NAudio library,
    /// and ensuring we are in PCM format
    /// </summary>
    /// <param name="fileName">File Name</param>
    private void CreateReaderStream(string fileName)
    {
        if (fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            _readerStream = new WaveFileReader(fileName);
            if (_readerStream.WaveFormat.Encoding == WaveFormatEncoding.Pcm ||
                _readerStream.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                return;
            _readerStream = WaveFormatConversionStream.CreatePcmStream(_readerStream);
            _readerStream = new BlockAlignReductionStream(_readerStream);
        }
        else if (fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            _readerStream = new Mp3FileReader(fileName);
        else if (fileName.EndsWith(".aiff", StringComparison.OrdinalIgnoreCase) ||
                 fileName.EndsWith(".aif", StringComparison.OrdinalIgnoreCase))
            _readerStream = new AiffFileReader(fileName);
        else
        {
            // fall back to media foundation reader, see if that can play it
            _readerStream = new MediaFoundationReader(fileName);
        }
    }

    /// <summary>
    /// File Name
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// WaveFormat of this stream
    /// </summary>
    public override WaveFormat WaveFormat => _sampleChannel.WaveFormat;

    /// <summary>
    /// Length of this stream (in bytes)
    /// </summary>
    public override long Length => _length;

    /// <summary>
    /// Position of this stream (in bytes)
    /// </summary>
    public override long Position
    {
        get => SourceToDest(_readerStream.Position);
        set { lock (_lockObject) _readerStream.Position = DestToSource(value); }
    }

    /// <summary>
    /// Gets or Sets the AddtionVolume of this AudioFileReader. 1.0f is full volume
    /// </summary>
    public float Volume
    {
        get => _sampleChannel.Volume;
        set => _sampleChannel.Volume = value;
    }

    /// <summary>
    /// Reads from this wave stream
    /// </summary>
    /// <param name="buffer">Audio buffer</param>
    /// <param name="offset">Offset into buffer</param>
    /// <param name="count">Number of bytes required</param>
    /// <returns>Number of bytes read</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        WaveBuffer waveBuffer = new WaveBuffer(buffer);
        int samplesRequired = count / 4;
        return Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired) * 4;
    }

    /// <summary>
    /// Reads audio from this sample provider
    /// </summary>
    /// <param name="buffer">Sample buffer</param>
    /// <param name="offset">Offset into sample buffer</param>
    /// <param name="count">Number of samples required</param>
    /// <returns>Number of samples read</returns>
    public int Read(float[] buffer, int offset, int count)
    {
        lock (_lockObject)
        {
            return _sampleChannel.Read(buffer, offset, count);
        }
    }

    /// <summary>
    /// Helper to convert source to dest bytes
    /// </summary>
    private long SourceToDest(long sourceBytes)
    {
        return _destBytesPerSample * (sourceBytes / _sourceBytesPerSample);
    }

    /// <summary>
    /// Helper to convert dest to source bytes
    /// </summary>
    private long DestToSource(long destBytes)
    {
        return _sourceBytesPerSample * (destBytes / _destBytesPerSample);
    }

    /// <summary>
    /// Disposes this AudioFileReader
    /// </summary>
    /// <param name="disposing">True if called from Dispose</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _readerStream != null)
        {
            _readerStream.Dispose();
            _readerStream = null;
            if (FileName?.EndsWith(".sound") == true) Task.Run(() => File.Delete(FileName));
        }
        base.Dispose(disposing);
    }
}