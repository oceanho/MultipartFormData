
using System;
using System.Collections.Generic;

namespace AimaTeam.Http
{
    /// <summary>
    /// This is a extension class for <see cref="MultipartFormData"/> 
    /// </summary>
    public static partial class MultipartFormDataExtension
    {
        /// <summary>
        /// 将一个字典添加到 <see cref="MultipartFormData"/>对象中
        /// </summary>
        /// <param name="formData"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static MultipartFormData AddParameter(this MultipartFormData formData, IDictionary<string, object> _params)
        {
            foreach (var _p in _params.Keys)
            {
                formData.AddParameter(_p, _params[_p] == null ? "" : _params[_p].ToString());
            }
            return formData;
        }

        /// <summary>
        /// 将一个字典添加到 <see cref="MultipartFormData"/>对象中
        /// </summary>
        /// <param name="formData"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static MultipartFormData AddParameter(this MultipartFormData formData, IDictionary<string, string> _params)
        {
            foreach (var _p in _params.Keys)
            {
                formData.AddParameter(_p, _params[_p] == null ? "" : _params[_p]);
            }
            return formData;
        }
    }
}
