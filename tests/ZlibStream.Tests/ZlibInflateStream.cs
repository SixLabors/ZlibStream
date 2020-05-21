using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ZlibStream.Tests
{
    public sealed class ZLIBStream : Stream
    {
        #region "Variables globales"
        private CompressionMode mCompressionMode = CompressionMode.Compress;
        private CompressionLevel mCompressionLevel = CompressionLevel.NoCompression;
        private bool mLeaveOpen = false;
        private Adler32 adler32 = new Adler32();
        private DeflateStream mDeflateStream;
        private Stream mRawStream;
        private bool mClosed = false;
        private byte[] mCRC = null;
        #endregion
        #region "Constructores"
        /// <summary>
        /// Inicializa una nueva instancia de la clase ZLIBStream usando la secuencia y nivel de compresión especificados.
        /// </summary>
        /// <param name="stream">Secuencia que se va a comprimir</param>
        /// <param name="compressionLevel">Nivel de compresión</param>
        public ZLIBStream(Stream stream, CompressionLevel compressionLevel) : this(stream, compressionLevel, false)
        {
        }
        /// <summary>
        /// Inicializa una nueva instancia de la clase ZLIBStream usando la secuencia y modo de compresión especificados.
        /// </summary>
        /// <param name="stream">Secuencia que se va a comprimir o descomprimir</param>
        /// <param name="compressionMode">Modo de compresión</param>
        public ZLIBStream(Stream stream, CompressionMode compressionMode) : this(stream, compressionMode, false)
        {
        }
        /// <summary>
        /// Inicializa una nueva instancia de la clase ZLIBStream usando la secuencia y nivel de compresión especificados y, opcionalmente, deja la secuencia abierta.
        /// </summary>
        /// <param name="stream">Secuencia que se va a comprimir</param>
        /// <param name="compressionLevel">Nivel de compresión</param>
        /// <param name="leaveOpen">Indica si se debe de dejar la secuencia abierta después de comprimir la secuencia</param>
        public ZLIBStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
        {
            this.mCompressionMode = CompressionMode.Compress;
            this.mCompressionLevel = compressionLevel;
            this.mLeaveOpen = leaveOpen;
            this.mRawStream = stream;
            this.InicializarStream();
        }
        /// <summary>
        /// Inicializa una nueva instancia de la clase ZLIBStream usando la secuencia y modo de compresión especificados y, opcionalmente, deja la secuencia abierta.
        /// </summary>
        /// <param name="stream">Secuencia que se va a comprimir o descomprimir</param>
        /// <param name="compressionMode">Modo de compresión</param>
        /// <param name="leaveOpen">Indica si se debe de dejar la secuencia abierta después de comprimir o descomprimir la secuencia</param>
        public ZLIBStream(Stream stream, CompressionMode compressionMode, bool leaveOpen)
        {
            this.mCompressionMode = compressionMode;
            this.mCompressionLevel = CompressionLevel.Fastest;
            this.mLeaveOpen = leaveOpen;
            this.mRawStream = stream;
            this.InicializarStream();
        }
        #endregion
        #region "Propiedades sobreescritas"
        public override bool CanRead
        {
            get
            {
                return ((this.mCompressionMode == CompressionMode.Decompress) && (this.mClosed != true));
            }
        }
        public override bool CanWrite
        {
            get
            {
                return ((this.mCompressionMode == CompressionMode.Compress) && (this.mClosed != true));
            }
        }
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }
        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        #endregion
        #region "Metodos sobreescritos"
        public override int ReadByte()
        {
            int result = 0;

            if (this.CanRead == true)
            {
                result = this.mDeflateStream.ReadByte();

                //Comprobamos si se ha llegado al final del stream
                if (result == -1)
                {
                    this.ReadCRC();
                }
                else
                {
                    this.adler32.Update(Convert.ToByte(result));
                }
            }
            else
            {
                throw new InvalidOperationException();
            }

            return result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = 0;

            if (this.CanRead == true)
            {
                result = this.mDeflateStream.Read(buffer, offset, count);

                //Comprobamos si se ha llegado al final del stream
                if (result < 1)
                {
                    this.ReadCRC();
                }
                else
                {
                    this.adler32.Update(buffer, offset, result);
                }
            }
            else
            {
                throw new InvalidOperationException();
            }

            return result;
        }

        public override void WriteByte(byte value)
        {
            if (this.CanWrite == true)
            {
                this.mDeflateStream.WriteByte(value);
                this.adler32.Update(value);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.CanWrite == true)
            {
                this.mDeflateStream.Write(buffer, offset, count);
                this.adler32.Update(buffer, offset, count);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public override void Close()
        {
            if (this.mClosed == false)
            {
                this.mClosed = true;
                if (this.mCompressionMode == CompressionMode.Compress)
                {
                    this.Flush();
                    this.mDeflateStream.Close();

                    this.mCRC = BitConverter.GetBytes(adler32.GetValue());

                    if (BitConverter.IsLittleEndian == true)
                    {
                        Array.Reverse(this.mCRC);
                    }

                    this.mRawStream.Write(this.mCRC, 0, this.mCRC.Length);
                }
                else
                {
                    this.mDeflateStream.Close();
                    if (this.mCRC == null)
                    {
                        this.ReadCRC();
                    }
                }

                if (this.mLeaveOpen == false)
                {
                    this.mRawStream.Close();
                }
            }
            else
            {
                throw new InvalidOperationException("Stream already closed");
            }
        }

        public override void Flush()
        {
            if (this.mDeflateStream != null)
            {
                this.mDeflateStream.Flush();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
        #endregion
        #region "Metodos publicos"
        /// <summary>
        /// Comprueba si el stream esta en formato ZLib
        /// </summary>
        /// <param name="stream">Stream a comprobar</param>
        /// <returns>Retorna True en caso de que el stream sea en formato ZLib y False en caso contrario u error</returns>
        public static bool IsZLibStream(Stream stream)
        {
            bool bResult = false;
            int CMF = 0;
            int Flag = 0;
            ZLibHeader header;

            //Comprobamos si la secuencia esta en la posición 0, de no ser así, lanzamos una excepción
            if (stream.Position != 0)
            {
                throw new ArgumentOutOfRangeException("Sequence must be at position 0");
            }

            //Comprobamos si podemos realizar la lectura de los dos bytes que conforman la cabecera
            if (stream.CanRead == true)
            {
                CMF = stream.ReadByte();
                Flag = stream.ReadByte();
                try
                {
                    header = ZLibHeader.DecodeHeader(CMF, Flag);
                    bResult = header.IsSupportedZLibStream;
                }
                catch
                {
                    //Nada
                }
            }

            return bResult;
        }
        /// <summary>
        /// Lee los últimos 4 bytes del stream ya que es donde está el CRC
        /// </summary>
        private void ReadCRC()
        {
            this.mCRC = new byte[4];
            this.mRawStream.Seek(-4, SeekOrigin.End);
            if (this.mRawStream.Read(this.mCRC, 0, 4) < 4)
            {
                throw new EndOfStreamException();
            }

            if (BitConverter.IsLittleEndian == true)
            {
                Array.Reverse(this.mCRC);
            }

            uint crcAdler = this.adler32.GetValue();
            uint crcStream = BitConverter.ToUInt32(this.mCRC, 0);

            if (crcStream != crcAdler)
            {
                throw new Exception("CRC mismatch");
            }
        }
        #endregion
        #region "Metodos privados"
        /// <summary>
        /// Inicializa el stream
        /// </summary>
        private void InicializarStream()
        {
            switch (this.mCompressionMode)
            {
                case CompressionMode.Compress:
                {
                    this.InicializarZLibHeader();
                    this.mDeflateStream = new DeflateStream(this.mRawStream, this.mCompressionLevel, true);
                    break;
                }
                case CompressionMode.Decompress:
                {
                    if (ZLIBStream.IsZLibStream(this.mRawStream) == false)
                    {
                        throw new InvalidDataException();
                    }
                    this.mDeflateStream = new DeflateStream(this.mRawStream, CompressionMode.Decompress, true);
                    break;
                }
            }
        }
        /// <summary>
        /// Inicializa el encabezado del stream en formato ZLib
        /// </summary>
        private void InicializarZLibHeader()
        {
            byte[] bytesHeader;

            //Establecemos la configuración de la cabecera
            ZLibHeader header = new ZLibHeader();

            header.CompressionMethod = 8; //Deflate
            header.CompressionInfo = 7;

            header.FDict = false; //Sin diccionario
            switch (this.mCompressionLevel)
            {
                case CompressionLevel.NoCompression:
                {
                    header.FLevel = FLevel.Faster;
                    break;
                }
                case CompressionLevel.Fastest:
                {
                    header.FLevel = FLevel.Default;
                    break;
                }
                case CompressionLevel.Optimal:
                {
                    header.FLevel = FLevel.Optimal;
                    break;
                }
            }

            bytesHeader = header.EncodeZlibHeader();

            this.mRawStream.WriteByte(bytesHeader[0]);
            this.mRawStream.WriteByte(bytesHeader[1]);
        }
        #endregion

        public enum FLevel
        {
            Faster = 0,
            Fast = 1,
            Default = 2,
            Optimal = 3,
        }
        public sealed class ZLibHeader
        {
            #region "Variables globales"
            private bool mIsSupportedZLibStream;
            private byte mCompressionMethod; //CMF 0-3
            private byte mCompressionInfo; //CMF 4-7
            private byte mFCheck; //Flag 0-4 (Check bits for CMF and FLG)
            private bool mFDict; //Flag 5 (Preset dictionary)
            private FLevel mFLevel; //Flag 6-7 (Compression level)
            #endregion
            #region "Propiedades"
            public bool IsSupportedZLibStream
            {
                get
                {
                    return this.mIsSupportedZLibStream;
                }
                set
                {
                    this.mIsSupportedZLibStream = value;
                }
            }
            public byte CompressionMethod
            {
                get
                {
                    return this.mCompressionMethod;
                }
                set
                {
                    if (value > 15)
                    {
                        throw new ArgumentOutOfRangeException("Argument cannot be greater than 15");
                    }
                    this.mCompressionMethod = value;
                }
            }
            public byte CompressionInfo
            {
                get
                {
                    return this.mCompressionInfo;
                }
                set
                {
                    if (value > 15)
                    {
                        throw new ArgumentOutOfRangeException("Argument cannot be greater than 15");
                    }
                    this.mCompressionInfo = value;
                }
            }
            public byte FCheck
            {
                get
                {
                    return this.mFCheck;
                }
                set
                {
                    if (value > 31)
                    {
                        throw new ArgumentOutOfRangeException("Argument cannot be greater than 31");
                    }
                    this.mFCheck = value;
                }
            }
            public bool FDict
            {
                get
                {
                    return this.mFDict;
                }
                set
                {
                    this.mFDict = value;
                }
            }
            public FLevel FLevel
            {
                get
                {
                    return this.mFLevel;
                }
                set
                {
                    this.mFLevel = value;
                }
            }
            #endregion
            #region "Constructor"
            public ZLibHeader()
            {

            }
            #endregion
            #region "Metodos privados"
            private void RefreshFCheck()
            {
                string bitsFLG = Convert.ToString(Convert.ToByte(this.FLevel), 2).PadLeft(2, '0') + Convert.ToString(Convert.ToByte(this.FDict), 2);
                byte byteFLG = Convert.ToByte(bitsFLG, 2);
                this.FCheck = Convert.ToByte(31 - Convert.ToByte((this.GetCMF() * 256 + byteFLG) % 31));
            }
            private byte GetCMF()
            {
                string bitsCMF = Convert.ToString(this.CompressionInfo, 2).PadLeft(4, '0') + Convert.ToString(this.CompressionMethod, 2).PadLeft(4, '0');
                return Convert.ToByte(bitsCMF, 2);
            }
            private byte GetFLG()
            {
                string bitsFLG = Convert.ToString(Convert.ToByte(this.FLevel), 2).PadLeft(2, '0') + Convert.ToString(Convert.ToByte(this.FDict), 2) + Convert.ToString(this.FCheck, 2).PadLeft(5, '0');
                return Convert.ToByte(bitsFLG, 2);
            }
            #endregion
            #region "Metodos publicos"
            public byte[] EncodeZlibHeader()
            {
                byte[] result = new byte[2];

                this.RefreshFCheck();

                result[0] = this.GetCMF();
                result[1] = this.GetFLG();

                return result;
            }
            #endregion
            #region "Metodos estáticos"
            public static ZLibHeader DecodeHeader(int pCMF, int pFlag)
            {
                ZLibHeader result = new ZLibHeader();

                if ((pCMF < byte.MinValue) || (pCMF > byte.MaxValue))
                {
                    throw new ArgumentOutOfRangeException("Argument 'CMF' must be a byte");
                }
                if ((pFlag < byte.MinValue) || (pFlag > byte.MaxValue))
                {
                    throw new ArgumentOutOfRangeException("Argument 'Flag' must be a byte");
                }

                string bitsCMF = Convert.ToString(pCMF, 2).PadLeft(8, '0');
                string bitsFlag = Convert.ToString(pFlag, 2).PadLeft(8, '0');

                result.CompressionInfo = Convert.ToByte(bitsCMF.Substring(0, 4), 2);
                result.CompressionMethod = Convert.ToByte(bitsCMF.Substring(4, 4), 2);

                result.FCheck = Convert.ToByte(bitsFlag.Substring(3, 5), 2);
                result.FDict = Convert.ToBoolean(Convert.ToByte(bitsFlag.Substring(2, 1), 2));
                result.FLevel = (FLevel)Convert.ToByte(bitsFlag.Substring(0, 2), 2);

                result.IsSupportedZLibStream = (result.CompressionMethod == 8) && (result.CompressionInfo == 7) && (((pCMF * 256 + pFlag) % 31 == 0));

                return result;
            }
            #endregion
        }
        public class Adler32
        {
            #region "Variables globales"
            private UInt32 a = 1;
            private UInt32 b = 0;
            private const int _base = 65521;
            private const int _nmax = 5550;
            private int pend = 0;
            #endregion
            #region "Metodos publicos"
            public void Update(byte data)
            {
                if (pend >= _nmax) updateModulus();
                a += data;
                b += a;
                pend++;
            }
            public void Update(byte[] data)
            {
                Update(data, 0, data.Length);
            }
            public void Update(byte[] data, int offset, int length)
            {
                int nextJToComputeModulus = _nmax - pend;
                for (int j = 0; j < length; j++)
                {
                    if (j == nextJToComputeModulus)
                    {
                        updateModulus();
                        nextJToComputeModulus = j + _nmax;
                    }
                    unchecked
                    {
                        a += data[j + offset];
                    }
                    b += a;
                    pend++;
                }
            }
            public void Reset()
            {
                a = 1;
                b = 0;
                pend = 0;
            }
            private void updateModulus()
            {
                a %= _base;
                b %= _base;
                pend = 0;
            }
            public UInt32 GetValue()
            {
                if (pend > 0) updateModulus();
                return (b << 16) | a;
            }
            #endregion
        }
    }
}
