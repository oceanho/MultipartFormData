# MultipartFormData
The MultipartFormData,It's support http request body build when the mediaType is multipart/form-data 

# How use it ?
download the MultipartFormData.cs and include to your project

and then

using AimaTeam.Http;

// param

var param_id = new { key = "id", value = 123456 };
var param_name = new { key = "name", value = "Mr-hai" };

// data

var data_file = new { key = "file", filename = "my.jpg", data = new byte[2] { 10, 11 } };

// must excute End() when all param/files Added

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

more infomations,you can find in MultipartFormData_sample.cs
