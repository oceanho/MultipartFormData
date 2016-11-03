using System;
using System.IO;
using System.Text;
using AimaTeam.Http;

namespace AimaTeam.Http.Sample
{
    public class MultipartFormData_sample
    {

        public void Sample_GetMultipartFormDataBytes()
        {
            // param
            var param_id = new { key = "id", value = 123456 };
            var param_name = new { key = "name", value = "Mr-hai" };

            // data
            var data_file = new { key = "file", filename = "my.jpg", data = new byte[2] { 10, 11 } };

            using (var formData = new MultipartFormData()
                .AddParameter(param_id.key, param_id.value.ToString())
                .AddParameter(param_name.key, param_name.value)
                .AddFileObject(data_file.key, data_file.filename, data_file.data)
                .End())
            {
                    // Get multipart/form-data as bytes
                    var bytes = formData.GetDataBytes();

                    // Get the multipart/form-data as Stream
                    var stream = formData.GetDataStream();

                    // Get the Boundary
                    var boundary = formData.Boundary;
            }
        }
    }
}
