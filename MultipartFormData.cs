using System;
using System.IO;
using System.Text;

namespace AimaTeam.Http
{
     /* *
     * 
     * Multipart/form-data 请求参数构建对象。可以构建一个完整的multipart/form-data所需要的请求参数
     * 
     * 
     * multipart/form-data 的参数格式
     * multipart/form-data 由一个boundary对发送内容进行分割
     * multipart form-data 之间用 boundary进行分开、
     * 每一个 multipart form-data 的 Disposition 和 数据部分 用 2个 \r\n 进行分开。
     * 每一个 multipart form-data 的 Disposition 和 ContentType 用 1个 \r\n 进行分开（multipart form-data 是文件对象一定有content-type）
     * 最后用 boundary-- 表示body结束，非最后一个 boundary 都需要在前面加一个 --
     * 
     * 示例
     * ${boundary}
     * Content-Disposition: form-data; name="id"
     * 
     * 123
     * --${boundary}
     * Content-Disposition: form-data; name="name";
     * 
     * mr-hai
     * --${boundary}
     * Content-Disposition: form-data; name="data"; filename="file1"
     * Content-Type: image/png
     * 
     * here is datas
     * --${boundary}--
     * 
     * **/
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class MultipartFormData : IDisposable
    {
        private int flag = 0;

        private string boundary;
        private Stream formStream;
        private Encoding encoding;

        private byte[] newLineBytes;
        private byte[] internalBoundaryBytes;
        private byte[] internalBoundaryStartOrEndBytes;

        private readonly static Encoding utf8_encodings = Encoding.UTF8;
        private readonly char[] invald_file_name_chars = new char[] { '\\', '/', '?', '"' };

        /// <summary>
        /// 
        /// </summary>
        public MultipartFormData() :
            this(utf8_encodings)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="encoding">非二进制类参数写入时的读取编码方式</param>
        public MultipartFormData(Encoding encoding) :
            this(new MemoryStream(), encoding)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="encoder">非二进制类参数写入时的读取编码方式</param>
        /// <param name="dataStream">multipart/form-data数据写入流对象</param>
        public MultipartFormData(Stream dataStream, Encoding encoder)
        {
            encoding = encoder;
            formStream = dataStream;

            boundary = "--OCEANHO" + DateTime.Now.Ticks.ToString("X");
            internalBoundaryBytes = Encoding.ASCII.GetBytes(boundary);

            newLineBytes = Encoding.ASCII.GetBytes(Environment.NewLine);
            internalBoundaryStartOrEndBytes = Encoding.ASCII.GetBytes("--");
        }

        /// <summary>
        /// 获取 Multipart form-data boundary
        /// </summary>
        public string Boundary
        {
            get
            {
                return boundary;
            }
        }

        /// <summary>
        /// 获取 Multipart form-data 的总长度
        /// </summary>
        public long Length
        {
            get
            {
                return formStream.Length;
            }
        }

        /// <summary>
        /// 获取Boundary的字节数组
        /// </summary>
        internal byte[] InternalBoundaryBytes
        {
            get
            {
                return internalBoundaryBytes;
            }
        }

        /// <summary>
        /// 开始（可选）
        /// </summary>
        /// <returns></returns>
        public void Begin()
        {
            if (flag == 0)
            {
                flag++;
                Pri_WriteBoundary();
            }
        }

        /// <summary>
        /// 结束（所有数据都完成后必须调用此函数）
        /// </summary>
        /// <returns></returns>
        public MultipartFormData End()
        {
            // 减去写入参数的结束boundary + 1个newline部分
            if (flag > 1)
                formStream.SetLength(formStream.Length - (internalBoundaryBytes.Length + newLineBytes.Length));

            // 写入结束boundary
            Pri_WriteEndBoundary();

            // 结束 \r\n
            Pri_WriteNewLine();
            return this;
        }
        /// <summary>
        /// 添加一个Multipart/form-data 参数
        /// </summary>
        /// <param name="key">参数键</param>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        public MultipartFormData AddParameter(string key, string value)
        {
            return AddParameter(key, encoding.GetBytes(value));
        }

        /// <summary>
        /// 添加一个Multipart/form-data 参数
        /// </summary>
        /// <param name="key">参数键</param>
        /// <param name="value">参数数组</param>
        /// <returns></returns>
        public MultipartFormData AddParameter(string key, byte[] value)
        {
            return AddParameter(key, value, 0);
        }

        /// <summary>
        /// 添加一个Multipart/form-data 参数
        /// </summary>
        /// <param name="key">参数键</param>
        /// <param name="value">参数数组</param>
        /// <param name="start">指定参数 value 写入数据的开始位置 </param>
        /// <returns></returns>
        public MultipartFormData AddParameter(string key, byte[] value, int start)
        {
            return AddParameter(key, value, start, value.Length - start);
        }

        /// <summary>
        /// 添加一个Multipart/form-data 参数
        /// </summary>
        /// <param name="key">参数键</param>
        /// <param name="value">参数数组</param>
        /// <param name="start">指定参数 value 写入数据的开始位置 </param>
        /// <param name="length">指定参数 value 写入数据的总长度 </param>
        /// <returns></returns>
        public MultipartFormData AddParameter(string key, byte[] value, int start, int length)
        {
            if (length - start > value.Length)
                throw new ArgumentOutOfRangeException("Invalid arguments,The length should be <= value.Length - start");

            if (start >= length)
                throw new ArgumentOutOfRangeException("Invalid arguments,The start should be < length");

            if (flag == 0)
                Begin();

            Pri_WriteNewLine();

            // Write("Content-Disposition: form-data; name=\"" + key + "\";");
            Pri_Write("Content-Disposition: form-data; name=\"" + key + "\"");
            Pri_WriteNewPartLine();

            if (start != 0 && length != value.Length)
            {
                var _value = new byte[length];
                Array.Copy(value, start, _value, 0, length);
                Pri_Write(_value);
            }
            else
            {
                Pri_Write(value);
            }

            // 写入下一个分隔符
            Pri_WriteNewLine();
            Pri_WriteBoundary();

            flag++;
            return this;
        }

        /// <summary>
        /// 添加一个Multipart/form-data 数据（文件字节数组或者数据块字节数组）
        /// </summary>
        /// <param name="key">参数名称</param>
        /// <param name="fileName">文件名，不能包括特殊字符（必须是正常的文件名称，不能包含路径。）</param>
        /// <param name="value">数据（文件的字节数组或者数据块字节数组）</param>
        /// <param name="mediaType">文件类型</param>        
        /// <returns></returns>
        public MultipartFormData AddFileObject(string key, string fileName, byte[] value, string mediaType = null)
        {
            return AddFileObject(key, fileName, value, 0, mediaType);
        }

        /// <summary>
        /// 添加一个Multipart/form-data 数据（文件字节数组或者数据块字节数组）
        /// </summary>
        /// <param name="key">参数名称</param>
        /// <param name="fileName">文件名，不能包括特殊字符（必须是正常的文件名称，不能包含路径。）</param>
        /// <param name="value">数据（文件的字节数组或者数据块字节数组）</param>
        /// <param name="start">指定参数 value 写入数据的开始位置 </param>
        /// <param name="mediaType">文件类型</param>        
        /// <returns></returns>
        public MultipartFormData AddFileObject(string key, string fileName, byte[] value, int start, string mediaType = null)
        {
            return AddFileObject(key, fileName, value, start, value.Length - start, mediaType);
        }

        /// <summary>
        /// 添加一个Multipart/form-data 数据（文件字节数组或者数据块字节数组）
        /// </summary>
        /// <param name="key">参数名称</param>
        /// <param name="fileName">文件名，不能包括特殊字符（必须是正常的文件名称，不能包含路径。）</param>
        /// <param name="value">数据（文件的字节数组或者数据块字节数组）</param>
        /// <param name="start">指定参数 value 写入数据的开始位置 </param>
        /// <param name="length">指定参数 value 写入数据的总长度 </param>
        /// <param name="mediaType">文件类型</param>        
        /// <returns></returns>
        public MultipartFormData AddFileObject(string key, string fileName, byte[] value, int start, int length, string mediaType = null)
        {
            if (fileName.IndexOfAny(invald_file_name_chars) > -1)
                throw new Exception("Invalid fileName,fileName may be include [" + string.Join(" , ", invald_file_name_chars) + "]");

            if (flag == 0)
                Begin();

            Pri_WriteNewLine();

            Pri_Write("Content-Disposition: form-data; name=\"" + key + "\"; filename=\"" + fileName + "\"");
            Pri_WriteNewLine();

            Pri_Write("Content-Type:" + (string.IsNullOrEmpty(mediaType) ? "application/octet-stream" : mediaType) + "");
            Pri_WriteNewPartLine();

            if (start != 0 && length != value.Length)
            {
                var _value = new byte[length];
                Array.Copy(value, start, _value, 0, length);
                Pri_Write(_value);
            }
            else
            {
                Pri_Write(value);
            }

            // 写入下一个分隔符
            Pri_WriteNewLine();
            Pri_WriteBoundary();

            flag++;
            return this;
        }

        /// <summary>
        /// 添加一个Multipart/form-data 数据（文件字节数组或者数据块字节数组）
        /// </summary>
        /// <param name="key">参数名称</param>
        /// <param name="fileObjName">文件名</param>
        /// <param name="stream">数据（文件的字节流或者数据块字节流）</param>
        /// <param name="mediaType">文件类型</param>        
        /// <returns></returns>
        public MultipartFormData AddFileObject(string key, string fileObjName, Stream stream, string mediaType = null)
        {
            return AddFileObject(key, fileObjName, Pri_GetDataBytes(formStream, 4096), mediaType);
        }

        /// <summary>
        /// 重置（此操作将导致 写入数据流的长度/Position都重置为零）
        /// </summary>
        /// <returns></returns>
        public MultipartFormData Reset()
        {
            flag = 0;
            ResetPosition(0);
            formStream.SetLength(0);
            return this;
        }

        /// <summary>
        /// 重置已经写入数据流的Position 为 <paramref name="position"/>
        /// </summary>
        /// <param name="position">设置流的位置参数值</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <returns></returns>
        public MultipartFormData ResetPosition(long position = 0)
        {
            if (formStream.Length <= position || position < 0)
                throw new ArgumentOutOfRangeException("Invalid arguments, The position should be between 0 and " + (formStream.Length > 0 ? formStream.Length - 1 : 0));
            formStream.Position = position;
            return this;
        }

        /// <summary>
        /// 获取写入了Multipart/form-data的数据流对象
        /// </summary>
        /// <returns></returns>
        public Stream GetDataStream()
        {
            return formStream;
        }

        /// <summary>
        /// 获取写入了Multipart/form-data的数据流字节数组
        /// </summary>
        /// <param name="bufferSize">每次读取的buffer长度</param>
        /// <returns></returns>
        public byte[] GetDataBytes(int bufferSize = 4096)
        {
            return Pri_GetDataBytes(formStream, bufferSize);
        }

        #region Impl -> IDisposeable

        /// <summary>
        /// impl IDispose
        /// </summary>
        public void Dispose()
        {
            if (formStream != null)
            {
                formStream.Dispose();
                formStream = null;
            }
            newLineBytes = null;
            internalBoundaryBytes = null;
            internalBoundaryStartOrEndBytes = null;            
        }
        #endregion

        #region helpers

        /// <summary>
        /// 获取写入了Multipart/form-data的数据流字节数组
        /// </summary>
        /// <param name="stream">获取流的流对象</param>
        /// <param name="bufferSize">每次读取的buffer长度</param>
        /// <returns></returns>
        private byte[] Pri_GetDataBytes(Stream stream, int bufferSize = 4096)
        {
            var buffer_size = 0;
            var buffer_index = 0;
            var buffer = new byte[stream.Length > bufferSize ? bufferSize : stream.Length];
            var buffer_result = new byte[stream.Length];
            while ((buffer_size = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                Array.Copy(buffer, 0, buffer_result, buffer_index, buffer_size);
                buffer_index += buffer_size;
            }
            return buffer_result;
        }

        /// <summary>
        /// 写入 指定 _encoding 编码 str 的 字节数组
        /// </summary>
        private void Pri_Write(string str)
        {
            Pri_Write(encoding.GetBytes(str));
        }

        /// <summary>
        /// 写入 指定 bytes
        /// </summary>
        private void Pri_Write(byte[] bytes)
        {
            formStream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// 写入 multipart 换行符
        /// </summary>
        private void Pri_WriteNewLine()
        {
            formStream.Write(newLineBytes, 0, newLineBytes.Length);
        }

        /// <summary>
        /// 写入multipart 的新内容分割符（2个\r\n）
        /// </summary>
        private void Pri_WriteNewPartLine()
        {
            Pri_WriteNewLine();
            Pri_WriteNewLine();
        }

        /// <summary>
        /// 写入 boundary
        /// </summary>
        private void Pri_WriteBoundary()
        {
            // 写入 boundary 前面 的  --
            formStream.Write(internalBoundaryStartOrEndBytes, 0, internalBoundaryStartOrEndBytes.Length);

            // 写入 boundary 
            formStream.Write(InternalBoundaryBytes, 0, InternalBoundaryBytes.Length);
        }

        /// <summary>
        /// 写入 结束的 boundary
        /// </summary>
        private void Pri_WriteEndBoundary()
        {
            Pri_WriteNewLine();
            Pri_WriteBoundary();
            formStream.Write(internalBoundaryStartOrEndBytes, 0, internalBoundaryStartOrEndBytes.Length);
        }
        #endregion
    }
}
